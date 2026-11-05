using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

/// <summary>
/// This tries to resolve a logger message template to its actual value, substituting the template's argument values where they're all known.
/// Whenever a value isn't known or the logic gets too complex/expensive, the message is measured 'as written'; see <see cref="LogMessageAsWrittenRenderer"/>.
/// A lot of the logic is to handle edge cases and avoid expensive operations, so that pathological code can't stall IDE or build analysis.
/// </summary>
internal sealed class LogMessageValueResolver(SemanticModel semanticModel, CancellationToken cancellationToken)
{
    /// <summary>
    /// string.Format throws a <see cref="FormatException"/> for alignments of this size or larger.
    /// </summary>
    private const int FormatWidthLimit = 1_000_000;

    /// <summary>
    /// The most operations resolved for one message, so pathological code can't stall IDE or build analysis.
    /// </summary>
    internal const int MaximumResolutionSteps = 1_000;

    /// <summary>
    /// The longest value resolved, so values that grow exponentially (e.g. locals that each double the previous one) can't exhaust memory.
    /// </summary>
    internal const int MaximumValueLength = 1_000;

    private readonly HashSet<ILocalSymbol> _resolvingLocals = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Completed resolutions of locals' initializers, which don't depend on where the local is referenced.
    /// A failure caused by a cycle between locals is cached too, as every local in (or depending on) a cycle fails whichever is resolved first.
    /// </summary>
    private readonly Dictionary<ILocalSymbol, (bool Resolved, object Value)> _localInitializers = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Completed checks that a local isn't changed after its declaration, excluding the per-reference position checks.
    /// </summary>
    private readonly Dictionary<ILocalSymbol, bool> _unchangedLocals = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// The locals declared in each block that may change after their declaration.
    /// </summary>
    private readonly Dictionary<BlockSyntax, HashSet<ISymbol>> _changeableLocals = [];

    private int _resolutionSteps;

    /// <summary>
    /// Once true, nothing else resolves and the message is skipped, as neither its value nor its 'as written' form can be trusted.
    /// The <see cref="LogMessageAsWrittenRenderer"/> checks this too, so it stops rendering as soon as the limit is reached.
    /// </summary>
    internal bool WorkLimitExceeded { get; private set; }

    /// <summary>
    /// Resolves a logger message template, substituting the template's argument values where they're all known.
    /// Where values aren't known, the message is measured as written; see <see cref="LogMessageAsWrittenRenderer"/>.
    /// </summary>
    public bool TryResolvingMessage(InvocationExpressionSyntax invocation, ExpressionSyntax message, out string value)
    {
        var resolved = TryResolvingMessageWithinLimit(invocation, message, out value);
        return resolved && !WorkLimitExceeded;
    }

    private bool TryResolvingMessageWithinLimit(InvocationExpressionSyntax invocation, ExpressionSyntax message, out string value)
    {
        if (!TryResolving(message, out value))
        {
            // The template's own value isn't known, so measure 'as written' if built from strings.
            // Its arguments aren't substituted, as the rendered text isn't the template's real value.
            return TryRenderingAsWritten(message, out value);
        }

        if (!MayHaveKnownArguments(invocation, value))
        {
            // The values can't be substituted, so the template is measured as written. This is checked without
            // getting the invocation's operation, as the first operation requested in a method builds the whole method's operation tree.
            value = LogMessageAsWrittenRenderer.TemplateAsWritten(value);
            return true;
        }

        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation call)
        {
            return false;
        }

        var values = new List<object>();

        foreach (var argument in call.Arguments.Where(argument => argument.Parameter?.IsParams == true))
        {
            if (!TryAddingArgument(argument, values))
            {
                value = LogMessageAsWrittenRenderer.TemplateAsWritten(value); // The argument values aren't all known, so use the 'as written' form.
                return true;
            }
        }

        if (values.Count == 0)
        {
            return true;
        }

        // If the values can't be substituted reliably (e.g. due to format specifiers), the template is measured as written.
        value = TryResolvingTemplate(value, [.. values], out var renderedMessage)
            ? renderedMessage
            : LogMessageAsWrittenRenderer.TemplateAsWritten(value);

        return true;
    }

    /// <summary>
    /// Resolves a message template whose values are only supplied at runtime (e.g. a <c>LoggerMessage.Define</c> format string),
    /// so it is always measured as written.
    /// </summary>
    public bool TryResolvingTemplateAsWritten(ExpressionSyntax template, out string value)
    {
        var resolved = TryResolvingTemplateAsWrittenWithinLimit(template, out value);
        return resolved && !WorkLimitExceeded;
    }

    private bool TryResolvingTemplateAsWrittenWithinLimit(ExpressionSyntax template, out string value)
    {
        if (TryResolving(template, out value))
        {
            value = LogMessageAsWrittenRenderer.TemplateAsWritten(value);
            return true;
        }

        return TryRenderingAsWritten(template, out value);
    }

    private bool TryResolving(ExpressionSyntax expression, out string value)
    {
        // Most messages are constants, which are resolved without getting the operation (see MayHaveKnownArguments).
        // A null constant resolves to an empty string, as TryResolvingValue does.
        var constant = semanticModel.GetConstantValue(expression, cancellationToken);

        if (constant.HasValue && constant.Value is null or string)
        {
            value = constant.Value as string ?? string.Empty;
            return true;
        }

        var resolved = TryResolvingValue(semanticModel.GetOperation(expression, cancellationToken), out var result);
        value = result as string;
        return resolved && value != null;
    }

    /// <summary>
    /// Checks whether a template's params argument values could be substituted, using only syntax and symbols.
    /// Where it returns <see langword="false"/>, resolving the arguments would give the template as written anyway: either
    /// the template has no placeholders, the method has no params parameter, or a params value is certainly unknown.
    /// </summary>
    private bool MayHaveKnownArguments(InvocationExpressionSyntax invocation, string template)
    {
        if (template.IndexOf('{') < 0)
        {
            return false; // Without placeholders, a template is always measured as written.
        }

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
        {
            return true;
        }

        var parameters = method.Parameters;

        if (parameters.Length == 0 || !parameters[parameters.Length - 1].IsParams)
        {
            return false; // There's no params parameter, so there are no values to substitute.
        }

        var arguments = invocation.ArgumentList.Arguments;

        if (arguments.Any(argument => argument.NameColon != null))
        {
            return true; // Named arguments don't map to parameters by position, so leave them to the operation.
        }

        // Positional arguments from the params parameter's position onwards are its values (or the array itself).
        for (var index = parameters.Length - 1; index < arguments.Count; index++)
        {
            if (IsCertainlyUnknown(arguments[index].Expression))
            {
                return false;
            }
        }

        return arguments.Count >= parameters.Length;
    }

    /// <summary>
    /// The most locals followed through their initializers by <see cref="IsCertainlyUnknown"/>, after which a value is
    /// left to the operation, so long chains of locals can't make the check itself expensive.
    /// </summary>
    private const int MaximumUnknownCheckDepth = 8;

    /// <summary>
    /// Checks for runtime values that <see cref="TryResolvingValue"/> never resolves, e.g. parameters, fields, properties,
    /// non-string arithmetic, calls to methods other than <see cref="string"/>'s, and locals initialized with any of these.
    /// Anything else may be resolvable, so returns <see langword="false"/>.
    /// </summary>
    private bool IsCertainlyUnknown(ExpressionSyntax expression, int depth = 0)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }

        if (semanticModel.GetConstantValue(expression, cancellationToken).HasValue)
        {
            return false;
        }

        switch (expression)
        {
            case MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or ConditionalAccessExpressionSyntax
                or ThisExpressionSyntax or BaseObjectCreationExpressionSyntax or ConditionalExpressionSyntax
                or CastExpressionSyntax or AwaitExpressionSyntax:
                return true;
            case BinaryExpressionSyntax binary:
                // Only string concatenation is resolved.
                return !binary.IsKind(SyntaxKind.AddExpression)
                    || semanticModel.GetTypeInfo(binary, cancellationToken).Type?.SpecialType != SpecialType.System_String;
            case InvocationExpressionSyntax invocation:
                // Only string's own methods are resolved.
                return semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol
                    is not IMethodSymbol { ContainingType.SpecialType: SpecialType.System_String };
            case IdentifierNameSyntax identifier:
                return semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is not ILocalSymbol local
                    || IsCertainlyUnknownLocal(local, depth);
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks whether a local can't be resolved, as it isn't initialized in a single declaration or its initializer is certainly unknown.
    /// </summary>
    private bool IsCertainlyUnknownLocal(ILocalSymbol local, int depth)
    {
        if (local.DeclaringSyntaxReferences.Length != 1
            || local.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not VariableDeclaratorSyntax { Initializer: { } initializer })
        {
            return true; // e.g. a foreach variable, out variable or pattern variable.
        }

        return depth < MaximumUnknownCheckDepth && IsCertainlyUnknown(initializer.Value, depth + 1);
    }

    /// <summary>
    /// Resolves an operation's exact value as a string, for use by <see cref="LogMessageAsWrittenRenderer"/>.
    /// A <see langword="null"/> constant resolves to an empty string, as it would in a concatenation.
    /// </summary>
    internal bool TryResolvingOperation(IOperation operation, out string value)
    {
        var resolved = TryResolvingValue(operation, out var result);
        value = resolved ? result as string ?? string.Empty : null;
        return resolved;
    }

    private bool TryRenderingAsWritten(ExpressionSyntax expression, out string value)
    {
        return new LogMessageAsWrittenRenderer(this)
            .TryRendering(semanticModel.GetOperation(expression, cancellationToken), out value);
    }

    private bool TryResolvingValue(IOperation operation, out object value)
    {
        value = null;

        if (operation == null)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (WorkLimitExceeded || ++_resolutionSteps > MaximumResolutionSteps)
        {
            WorkLimitExceeded = true;
            return false;
        }

        if (operation.Type?.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        if (operation is IConversionOperation conversion && conversion.IsImplicit && conversion.OperatorMethod == null
            && !conversion.Conversion.IsNumeric)
        {
            return TryResolvingValue(conversion.Operand, out value);
        }

        if (operation.ConstantValue.HasValue)
        {
            value = operation.ConstantValue.Value;

            if (value == null)
            {
                value = string.Empty; // If this doesn't throw an exception, the null constant is used in a string context, so it is treated as an empty string.
            }
            else if (value is bool or char or int or long or float or double)
            {
                value = value.ToString();
            }

            return value == null || value is string;
        }

        return TryResolvingExpression(operation, out value);
    }

    private bool TryResolvingExpression(IOperation operation, out object value)
    {
        value = null;

        var resolved = operation switch
        {
            ILocalReferenceOperation local
                => TryResolvingLocal(local, out value),
            IParenthesizedOperation parentheses
                => TryResolvingValue(parentheses.Operand, out value),
            IBinaryOperation binary when binary.OperatorKind == BinaryOperatorKind.Add && binary.OperatorMethod == null && binary.Type?.SpecialType == SpecialType.System_String
                => TryResolvingConcatenation(binary, out value),
            IInterpolatedStringOperation interpolation
                => TryResolvingInterpolation(interpolation, out value),
            IInvocationOperation call when call.TargetMethod.ContainingType.SpecialType == SpecialType.System_String
                => TryResolvingCall(call, out value),
            _ => false,
        };

        return resolved && IsWithinLengthLimit((value as string)?.Length ?? 0);
    }

    /// <summary>
    /// Checks a value's length against <see cref="MaximumValueLength"/>, marking the work limit as exceeded if it is too long.
    /// </summary>
    private bool IsWithinLengthLimit(long length)
    {
        if (length > MaximumValueLength)
        {
            WorkLimitExceeded = true;
        }

        return !WorkLimitExceeded;
    }

    /// <summary>
    /// Resolves a local's initializer if it is unchanged, guarding against recursive references between locals.
    /// </summary>
    private bool TryResolvingLocal(ILocalReferenceOperation reference, out object value)
    {
        value = null;
        var local = reference.Local;

        if (local.DeclaringSyntaxReferences.Length != 1
            || local.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not VariableDeclaratorSyntax declaration
            || declaration.Initializer == null)
        {
            return false; // The local isn't initialized in a single declaration, so we can't resolve its value.
        }

        // Resolve the initializer before the data-flow checks, as most initializers aren't constant
        // and resolving them is far cheaper than analysing the enclosing block.
        if (!TryResolvingInitializer(local, declaration, out var initialValue)
            || !IsUnchangedLocal(local, declaration, reference.Syntax))
        {
            return false; // The initializer isn't constant, or the local is modified after initialization, so we can't resolve its value.
        }

        value = initialValue;
        return true;
    }

    /// <summary>
    /// Resolves a local's initializer once, so locals referenced many times (e.g. <c>var b = a + a;</c>) aren't re-resolved.
    /// </summary>
    private bool TryResolvingInitializer(ILocalSymbol local, VariableDeclaratorSyntax declaration, out object value)
    {
        if (_localInitializers.TryGetValue(local, out var cached))
        {
            value = cached.Value;
            return cached.Resolved;
        }

        if (!_resolvingLocals.Add(local))
        {
            value = null;
            return false; // Prevent infinite recursion when a local is initialized with itself, or another local that references it.
        }

        try
        {
            var resolved = TryResolvingValue(semanticModel.GetOperation(declaration.Initializer.Value, cancellationToken), out value);

            if (!WorkLimitExceeded)
            {
                // Results are only cached when complete; once the work limit is exceeded, nothing resolves anyway.
                _localInitializers[local] = (resolved, value);
            }

            return resolved;
        }
        finally
        {
            _resolvingLocals.Remove(local);
        }
    }

    /// <summary>
    /// Checks that a local's initializer can be trusted by excluding later writes, captures, and reference escapes.
    /// </summary>
    private bool IsUnchangedLocal(ILocalSymbol local, VariableDeclaratorSyntax declaration, SyntaxNode reference)
    {
        if (local.RefKind != RefKind.None
            || declaration.Parent?.Parent is not LocalDeclarationStatementSyntax statement
            || statement.Declaration.Variables.Count != 1
            || statement.Parent is not BlockSyntax block
            || declaration.Span.End >= reference.SpanStart || !block.Span.Contains(reference.Span))
        {
            return false; // The local is either a ref, declared elsewhere, or referenced before its declaration, so we can't guarantee its value is unchanged.
        }

        // The remaining check doesn't depend on where the local is referenced, so is only run once per local.
        if (!_unchangedLocals.TryGetValue(local, out var unchanged))
        {
            unchanged = !GetChangeableLocals(block).Contains(local);
            _unchangedLocals[local] = unchanged;
        }

        return unchanged;
    }

    /// <summary>
    /// Finds the locals declared directly in a block that could change after their declaration: those written to
    /// (assigned, incremented, deconstructed into, or passed as <c>out</c>), aliased or passed by reference,
    /// or captured by a lambda or local function. Writes before the declaration aren't possible,
    /// so the whole block is checked; this is conservative, as writes after the reference are included too.
    /// <para>
    /// This is a single syntax walk per block, only binding identifiers that share a name with one of its locals.
    /// It is far cheaper than data-flow analysis, which would need running once for the block and once per local.
    /// </para>
    /// </summary>
    private HashSet<ISymbol> GetChangeableLocals(BlockSyntax block)
    {
        if (_changeableLocals.TryGetValue(block, out var changeable))
        {
            return changeable;
        }

        changeable = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        _changeableLocals[block] = changeable;

        var names = new HashSet<string>(block.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(statement => statement.Declaration.Variables)
            .Select(variable => variable.Identifier.ValueText));

        // An explicit stack, rather than recursion, so deeply nested expressions can't overflow the call stack.
        var pending = new Stack<(SyntaxNode Node, bool InFunction)>();
        pending.Push((block, false));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (node, inFunction) = pending.Pop();

            foreach (var child in node.ChildNodes())
            {
                var childInFunction = inFunction || child is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

                if (child is IdentifierNameSyntax identifier
                    && names.Contains(identifier.Identifier.ValueText)
                    && (childInFunction || IsChangedAt(identifier))
                    && semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is ILocalSymbol local)
                {
                    changeable.Add(local);
                }

                pending.Push((child, childInFunction));
            }
        }

        return changeable;
    }

    /// <summary>
    /// Checks whether an identifier is somewhere its variable could be written to or aliased.
    /// </summary>
    private static bool IsChangedAt(IdentifierNameSyntax identifier)
    {
        var expression = GetOutermost(identifier);

        switch (expression.Parent)
        {
            case RefExpressionSyntax or MakeRefExpressionSyntax:
            case ArgumentSyntax { RefKindKeyword.RawKind: not 0 }:
            case AssignmentExpressionSyntax assignment when assignment.Left == expression:
            case PrefixUnaryExpressionSyntax prefix when prefix.Kind() is SyntaxKind.PreIncrementExpression
                or SyntaxKind.PreDecrementExpression or SyntaxKind.AddressOfExpression:
            case PostfixUnaryExpressionSyntax postfix when postfix.Kind() is SyntaxKind.PostIncrementExpression
                or SyntaxKind.PostDecrementExpression:
                return true;
            case ArgumentSyntax { Parent: TupleExpressionSyntax tuple }:
                return IsDeconstructionTarget(tuple);
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks whether a tuple is (part of) the target of a deconstruction, e.g. <c>(a, (b, c)) = ...</c>.
    /// </summary>
    private static bool IsDeconstructionTarget(TupleExpressionSyntax tuple)
    {
        var target = GetOutermost(tuple);

        while (target.Parent is ArgumentSyntax { Parent: TupleExpressionSyntax outer })
        {
            target = GetOutermost(outer);
        }

        return target.Parent is AssignmentExpressionSyntax assignment && assignment.Left == target;
    }

    private static SyntaxNode GetOutermost(SyntaxNode expression)
    {
        while (expression.Parent is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses;
        }

        return expression;
    }

    private bool TryResolvingConcatenation(IBinaryOperation binary, out object value)
    {
        value = null;

        if (!TryResolvingValue(binary.LeftOperand, out var left) || !TryResolvingValue(binary.RightOperand, out var right))
        {
            return false; // One of the operands isn't a constant string, so we can't resolve the concatenation.
        }

        if (!IsWithinLengthLimit(LengthOf(left) + LengthOf(right)))
        {
            return false; // Checked before concatenating, so the result is never allocated.
        }

        value = string.Concat(left as string, right as string);
        return true;
    }

    private static long LengthOf(object value) => (value as string)?.Length ?? 0;

    private bool TryResolvingInterpolation(IInterpolatedStringOperation interpolation, out object value)
    {
        value = null;
        var result = new StringBuilder();

        foreach (var part in interpolation.Parts)
        {
            if (!TryResolvingInterpolationPart(part, out var text))
            {
                return false; // One of the parts isn't a constant string, so we can't resolve the interpolation.
            }

            result.Append(text);
        }

        value = result.ToString();
        return true;
    }

    /// <summary>
    /// Resolves literal text or a known interpolation value with supported formatting.
    /// </summary>
    internal bool TryResolvingInterpolationPart(IInterpolatedStringContentOperation part, out string text)
    {
        text = null;

        if (part is IInterpolatedStringTextOperation literal)
        {
            text = literal.Text.ConstantValue.Value as string;
            return text != null;
        }

        if (part is not IInterpolationOperation hole || !TryResolvingValue(hole.Expression, out var value))
        {
            return false; // The hole isn't a constant string, so we can't resolve the interpolation.
        }

        if (hole.FormatString != null)
        {
            return false; // Values may already be strings, so type-specific formatting cannot be evaluated reliably.
        }

        var format = "{0";

        if (hole.Alignment != null)
        {
            if (!hole.Alignment.ConstantValue.HasValue || hole.Alignment.ConstantValue.Value is not int alignment)
            {
                return false; // The alignment isn't a constant integer, so we can't resolve the interpolation.
            }

            format += "," + alignment;
        }

        return TryFormatting(format + "}", [value], out text);
    }

    /// <summary>
    /// Evaluates supported string.Concat and string.Format calls using known argument values in parameter order.
    /// </summary>
    private bool TryResolvingCall(IInvocationOperation call, out object value)
    {
        value = null;
        var values = new List<object>();

        foreach (var argument in call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal))
        {
            if (!TryAddingArgument(argument, values))
            {
                return false; // One of the arguments isn't a constant string, so we can't resolve the call.
            }
        }

        if (call.TargetMethod.Name == nameof(string.Concat))
        {
            if (!IsWithinLengthLimit(values.Sum(LengthOf)))
            {
                return false; // Checked before concatenating, so the result is never allocated.
            }

            value = string.Concat(values.Select(item => item as string));
            return true; // The call is a string.Concat call with constant string arguments, so we can resolve it.
        }

        if (call.TargetMethod.Name == nameof(string.Join)
            && call.TargetMethod.Parameters.Length == 2
            && call.TargetMethod.Parameters[1].IsParams
            && values.Count > 0)
        {
            if (!IsWithinLengthLimit(values.Skip(1).Sum(LengthOf) + (LengthOf(values[0]) * Math.Max(values.Count - 2, 0))))
            {
                return false; // Checked before joining, so the result is never allocated.
            }

            value = string.Join(values[0] as string, values.Skip(1).Select(item => item as string));
            return true;
        }

        if (call.TargetMethod.Name != nameof(string.Format) || values.Count < 2
            || call.TargetMethod.Parameters[0].Type.SpecialType != SpecialType.System_String
            || values[0] is not string format)
        {
            return false; // The call isn't a string.Format call with a constant format string, so we can't resolve it.
        }

        var success = TryFormatting(format, [.. values.Skip(1)], out var result);
        value = result;
        return success;
    }

    /// <summary>
    /// The operation kind used by C# 12+ collection expressions, including implicitly for C# 13 params collections.
    /// It is looked up by name because this compiles against a Roslyn version that predates it; on older hosts it is unavailable.
    /// </summary>
    private static readonly OperationKind? CollectionExpressionKind =
        Enum.TryParse<OperationKind>("CollectionExpression", out var kind) ? kind : null;

    /// <summary>
    /// Gets the elements passed to a params parameter, whether as an inline array or as a params collection.
    /// </summary>
    internal static bool TryGettingParamsElements(IOperation value, out IEnumerable<IOperation> elements)
    {
        elements = null;

        if (value is IArrayCreationOperation { Initializer: not null } array)
        {
            elements = array.Initializer.ElementValues;
            return true;
        }

        if (CollectionExpressionKind.HasValue && value?.Kind == CollectionExpressionKind.Value)
        {
            // A collection expression's child operations are its elements; spread elements won't resolve to a value.
            elements = value.ChildOperations;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds a resolved argument value, expanding inline params arrays and collections into their individual elements.
    /// </summary>
    private bool TryAddingArgument(IArgumentOperation argument, List<object> values)
    {
        if (argument.Parameter == null)
        {
            return false;
        }

        if (argument.Parameter.IsParams)
        {
            if (!TryGettingParamsElements(argument.Value, out var elements))
            {
                return false; // The params argument isn't an inline array or collection, so we can't resolve the message.
            }

            foreach (var element in elements)
            {
                if (!TryResolvingValue(element, out var item))
                {
                    return false; // One of the elements isn't a constant string, so we can't resolve the message.
                }

                values.Add(item);
            }

            return true;
        }

        if (!TryResolvingValue(argument.Value, out var value))
        {
            return false; // The argument isn't a constant string, so we can't resolve the message.
        }

        values.Add(value);
        return true;
    }

    /// <summary>
    /// Converts named logging placeholders to positional format items and substitutes known values.
    /// Preserves escaped braces and alignment, skipping format specifiers that cannot be evaluated reliably.
    /// </summary>
    private static bool TryResolvingTemplate(string template, object[] values, out string value)
    {
        value = null;
        var format = new StringBuilder();
        var argumentIndex = 0;

        for (var index = 0; index < template.Length; index++)
        {
            var character = template[index];

            if ((character == '{' || character == '}') && index + 1 < template.Length && template[index + 1] == character)
            {
                format.Append(character).Append(character);
                index++;
            }
            else if (character == '{')
            {
                if (!TryAppendingPlaceholder(template, format, argumentIndex++, ref index))
                {
                    return false; // The placeholder isn't valid, so we can't resolve the template.
                }
            }
            else
            {
                format.Append(character);
            }
        }

        return argumentIndex == values.Length
            && TryFormatting(format.ToString(), values.Select(item => item ?? "(null)").ToArray(), out value);
    }

    private static bool TryAppendingPlaceholder(string template, StringBuilder format, int argumentIndex, ref int index)
    {
        var end = template.IndexOf('}', index + 1);

        if (end < 0)
        {
            return false;
        }

        var placeholder = template.Substring(index + 1, end - index - 1);

        if (placeholder.IndexOf(':') >= 0)
        {
            return false; // Values may already be strings, so type-specific formatting cannot be evaluated reliably.
        }

        // Keep any alignment (e.g. "{Value,10}"), replacing the name with the positional index.
        var alignment = placeholder.IndexOf(',');
        format.Append('{').Append(argumentIndex);

        if (alignment >= 0)
        {
            format.Append(placeholder.Substring(alignment));
        }

        format.Append('}');

        index = end;
        return true;
    }

    private static bool TryFormatting(string format, object[] values, out string value)
    {
        value = null;

        if (!TryLimitingFormat(format, out var limitedFormat))
        {
            return false;
        }

        try
        {
            value = string.Format(limitedFormat, values);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks the format only has items that can be evaluated reliably, capping each item's alignment at
    /// <see cref="LogMessagesMinimumLengthAnalyzer.MaximumSettingValue"/> so it can't allocate excessive padding.
    /// Capping doesn't change whether a message is long enough: padding at either end is trimmed either way.
    /// </summary>
    private static bool TryLimitingFormat(string format, out string limitedFormat)
    {
        limitedFormat = null;
        var result = new StringBuilder(format.Length);

        for (var index = 0; index < format.Length; index++)
        {
            var character = format[index];

            if (character != '{' && character != '}')
            {
                result.Append(character);
                continue;
            }

            if (index + 1 < format.Length && format[index + 1] == character)
            {
                result.Append(character).Append(character);
                index++;
                continue;
            }

            var end = format.IndexOf('}', index + 1);

            if (character == '}' || end < 0 || format.IndexOf(':', index + 1, end - index - 1) >= 0)
            {
                // Values may already be strings, so type-specific formatting cannot be evaluated reliably.
                return false;
            }

            result.Append('{').Append(LimitAlignment(format.Substring(index + 1, end - index - 1))).Append('}');
            index = end;
        }

        limitedFormat = result.ToString();
        return true;
    }

    /// <summary>
    /// Caps the alignment of a format item (e.g. "0,999999" becomes "0,100").
    /// Anything other than a plain integer alignment is left unchanged, so invalid items still fail to format.
    /// </summary>
    private static string LimitAlignment(string item)
    {
        var comma = item.IndexOf(',');

        if (comma < 0)
        {
            return item;
        }

        var alignment = item.Substring(comma + 1).Trim();
        var negative = alignment.StartsWith("-", StringComparison.Ordinal);
        var digits = negative ? alignment.Substring(1) : alignment;
        var limit = LogMessagesMinimumLengthAnalyzer.MaximumSettingValue;

        if (digits.Length == 0
            || !digits.All(digit => digit >= '0' && digit <= '9')
            || !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var width)
            || width <= limit
            || width >= FormatWidthLimit)
        {
            // Leave alignments that are small enough, or that string.Format rejects without allocating, unchanged.
            return item;
        }

        return item.Substring(0, comma + 1) + (negative ? "-" : string.Empty) + limit.ToString(CultureInfo.InvariantCulture);
    }
}
