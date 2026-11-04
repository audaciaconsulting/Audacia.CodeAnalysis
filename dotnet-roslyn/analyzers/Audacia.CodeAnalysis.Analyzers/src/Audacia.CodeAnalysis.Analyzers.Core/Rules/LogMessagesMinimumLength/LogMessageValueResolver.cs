using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

internal sealed class LogMessageValueResolver(SemanticModel semanticModel, CancellationToken cancellationToken)
{
    private readonly HashSet<ILocalSymbol> _resolvingLocals = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Resolves a logger message template, substituting the template's argument values where they're all known.
    /// Where values aren't known, the message is measured as written; see <see cref="LogMessageAsWrittenRenderer"/>.
    /// </summary>
    public bool TryResolvingMessage(InvocationExpressionSyntax invocation, ExpressionSyntax message, out string value)
    {
        if (!TryResolving(message, out value))
        {
            // The template's own value isn't known, so measure 'as written' if built from strings.
            // Its arguments aren't substituted, as the rendered text isn't the template's real value.
            return TryRenderingAsWritten(message, out value);
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
        if (TryResolving(template, out value))
        {
            value = LogMessageAsWrittenRenderer.TemplateAsWritten(value);
            return true;
        }

        return TryRenderingAsWritten(template, out value);
    }

    public bool TryResolving(ExpressionSyntax expression, out string value)
    {
        var resolved = TryResolvingValue(semanticModel.GetOperation(expression, cancellationToken), out var result);
        value = result as string;
        return resolved && value != null;
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

        if (operation is IConversionOperation conversion && conversion.IsImplicit && conversion.OperatorMethod == null
            && !conversion.Conversion.IsNumeric)
        {
            return TryResolvingValue(conversion.Operand, out value);
        }

        if (operation.ConstantValue.HasValue)
        {
            value = operation.ConstantValue.Value;

            if (value is bool or char or int or long or float or double)
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

        return operation switch
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
    }

    /// <summary>
    /// Resolves a local's initializer if it is unchanged, guarding against recursive references between locals.
    /// </summary>
    private bool TryResolvingLocal(ILocalReferenceOperation reference, out object value)
    {
        value = null;
        var local = reference.Local;

        if (!_resolvingLocals.Add(local))
        {
            return false; // Prevent infinite recursion when a local is initialized with itself, or another local that references it.
        }

        try
        {
            if (local.DeclaringSyntaxReferences.Length != 1
                || local.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not VariableDeclaratorSyntax declaration
                || declaration.Initializer == null)
            {
                return false; // The local isn't initialized in a single declaration, so we can't resolve its value.
            }

            // Resolve the initializer before the data-flow checks, as most initializers aren't constant
            // and resolving them is far cheaper than analysing the enclosing block.
            if (!TryResolvingValue(semanticModel.GetOperation(declaration.Initializer.Value, cancellationToken), out var initialValue)
                || !IsUnchangedLocal(local, declaration, reference.Syntax))
            {
                return false; // The initializer isn't constant, or the local is modified after initialization, so we can't resolve its value.
            }

            value = initialValue;
            return true;
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

        if (HasReferenceEscape(block, local))
        {
            return false; // The local is referenced by ref, so we can't guarantee its value is unchanged.
        }

        // Data-flow analysis covers the whole enclosing block, so it runs only after the cheaper checks above have passed.
        var flow = semanticModel.AnalyzeDataFlow(block);

        if (flow == null || !flow.Succeeded || flow.Captured.Contains(local, SymbolEqualityComparer.Default))
        {
            return false; // The local is either captured by a lambda or referenced by ref, so we can't guarantee its value is unchanged.
        }

        var next = block.Statements.IndexOf(statement) + 1;

        if (next == block.Statements.Count)
        {
            return true; // The local is declared in the last statement of the block, so it can't be modified after its declaration.
        }

        var remainingFlow = semanticModel.AnalyzeDataFlow(block.Statements[next], block.Statements.Last());

        return remainingFlow != null && remainingFlow.Succeeded
            && !remainingFlow.WrittenInside.Contains(local, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Detects reference aliases and by-reference arguments that could allow a local's value to change indirectly.
    /// </summary>
    private bool HasReferenceEscape(BlockSyntax block, ILocalSymbol local)
    {
        foreach (var identifier in block.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            SyntaxNode expression = GetOutermostParenthesized(identifier);

            if (expression.Parent is not RefExpressionSyntax
                && expression.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: not 0 })
            {
                continue; // The identifier isn't passed by reference, so it can't escape.
            }

            if (SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, local))
            {
                return true; // The identifier is passed by reference, so the local can escape.
            }
        }

        return false;
    }

    private static SyntaxNode GetOutermostParenthesized(IdentifierNameSyntax identifier)
    {
        SyntaxNode expression = identifier;

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

        value = string.Concat(left as string, right as string);
        return true;
    }

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
            value = string.Concat(values.Select(item => item as string));
            return true; // The call is a string.Concat call with constant string arguments, so we can resolve it.
        }

        if (call.TargetMethod.Name == nameof(string.Join)
            && call.TargetMethod.Parameters.Length == 2
            && call.TargetMethod.Parameters[1].IsParams
            && values.Count > 0)
        {
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

        if (!IsSupportedFormat(format))
        {
            return false;
        }

        try
        {
            value = string.Format(format, values);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsSupportedFormat(string format)
    {
        for (var index = 0; index < format.Length; index++)
        {
            var character = format[index];

            if (character != '{' && character != '}')
            {
                continue;
            }

            if (index + 1 < format.Length && format[index + 1] == character)
            {
                index++;
                continue;
            }

            var end = format.IndexOf('}', index + 1);

            if (character == '}' || end < 0 || format.IndexOf(':', index + 1, end - index - 1) >= 0)
            {
                // Values may already be strings, so type-specific formatting cannot be evaluated reliably.
                return false;
            }

            index = end;
        }

        return true;
    }
}
