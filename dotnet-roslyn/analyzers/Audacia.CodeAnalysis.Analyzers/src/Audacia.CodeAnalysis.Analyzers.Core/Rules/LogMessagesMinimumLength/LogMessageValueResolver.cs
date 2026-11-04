using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

internal sealed class LogMessageValueResolver(SemanticModel semanticModel, CancellationToken cancellationToken)
{
    private readonly SemanticModel _semanticModel = semanticModel;
    private readonly CancellationToken _cancellationToken = cancellationToken;
    private readonly HashSet<ILocalSymbol> _resolvingLocals = new(SymbolEqualityComparer.Default);

    public bool TryResolvingMessage(InvocationExpressionSyntax invocation, ExpressionSyntax message, out string value)
    {
        if (!TryResolving(message, out value)
            || _semanticModel.GetOperation(invocation, _cancellationToken) is not IInvocationOperation call)
        {
            return false;
        }

        var values = new List<object>();

        foreach (var argument in call.Arguments.Where(argument => argument.Parameter?.IsParams == true))
        {
            if (!TryAddingArgument(argument, values))
            {
                return false; // The params argument isn't a constant array, so we can't resolve the message.
            }
        }

        return values.Count == 0 || TryResolvingTemplate(value, [.. values], out value);
    }

    public bool TryResolving(ExpressionSyntax expression, out string value)
    {
        var resolved = TryResolvingValue(_semanticModel.GetOperation(expression, _cancellationToken), out var result);
        value = result as string;
        return resolved && value != null;
    }

    private bool TryResolvingValue(IOperation operation, out object value)
    {
        value = null;

        if (operation == null)
        {
            return false;
        }

        _cancellationToken.ThrowIfCancellationRequested();

        if (operation is IConversionOperation conversion && conversion.IsImplicit && conversion.OperatorMethod == null)
        {
            return TryResolvingValue(conversion.Operand, out value);
        }

        if (operation.ConstantValue.HasValue)
        {
            value = operation.ConstantValue.Value;
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
                || local.DeclaringSyntaxReferences[0].GetSyntax(_cancellationToken) is not VariableDeclaratorSyntax declaration
                || declaration.Initializer == null || !IsUnchangedLocal(local, declaration, reference.Syntax))
            {
                return false; // The local is either not initialized or modified after initialization, so we can't resolve its value.
            }

            return TryResolvingValue(_semanticModel.GetOperation(declaration.Initializer.Value, _cancellationToken), out value);
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

        var flow = _semanticModel.AnalyzeDataFlow(block);

        if (flow == null || !flow.Succeeded || flow.Captured.Contains(local, SymbolEqualityComparer.Default) || HasReferenceEscape(block, local))
        {
            return false; // The local is either captured by a lambda or referenced by ref, so we can't guarantee its value is unchanged.
        }

        var next = block.Statements.IndexOf(statement) + 1;

        if (next == block.Statements.Count)
        {
            return true; // The local is declared in the last statement of the block, so it can't be modified after its declaration.
        }

        var remainingFlow = _semanticModel.AnalyzeDataFlow(block.Statements[next], block.Statements.Last());

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
            _cancellationToken.ThrowIfCancellationRequested();
            SyntaxNode expression = SetExpression(identifier);

            if (expression.Parent is not RefExpressionSyntax
                && expression.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: not 0 })
            {
                continue; // The identifier isn't passed by reference, so it can't escape.
            }

            if (SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(identifier, _cancellationToken).Symbol, local))
            {
                return true; // The identifier is passed by reference, so the local can escape.
            }
        }

        return false;
    }

    private static SyntaxNode SetExpression(IdentifierNameSyntax identifier)
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
    /// Resolves literal text or a known interpolation value, applying any alignment and format specifier.
    /// </summary>
    private bool TryResolvingInterpolationPart(IInterpolatedStringContentOperation part, out string text)
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

        var format = "{0";

        if (hole.Alignment != null)
        {
            if (!hole.Alignment.ConstantValue.HasValue || hole.Alignment.ConstantValue.Value is not int alignment)
            {
                return false; // The alignment isn't a constant integer, so we can't resolve the interpolation.
            }

            format += "," + alignment;
        }

        format += hole.FormatString == null ? "}" : ":" + hole.FormatString.ConstantValue.Value + "}";
        return TryFormatting(format, [value], out text);
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
    /// Adds a resolved argument value, expanding inline params arrays into their individual elements.
    /// </summary>
    private bool TryAddingArgument(IArgumentOperation argument, List<object> values)
    {
        if (argument.Parameter == null)
        {
            return false;
        }

        if (argument.Parameter.IsParams)
        {
            if (argument.Value is not IArrayCreationOperation { Initializer: not null } array)
            {
                return false; // The params argument isn't a constant array, so we can't resolve the message.
            }

            foreach (var element in array.Initializer.ElementValues)
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
    /// Preserves escaped braces, alignment, and format specifiers when calculating the resulting message.
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

        return argumentIndex == values.Length && TryFormatting(format.ToString(), values, out value);
    }

    private static bool TryAppendingPlaceholder(string template, StringBuilder format, int argumentIndex, ref int index)
    {
        var end = template.IndexOf('}', index + 1);

        if (end < 0)
        {
            return false;
        }

        var placeholder = template.Substring(index + 1, end - index - 1);
        AppendToFormat(format, argumentIndex, placeholder);

        index = end;
        return true;
    }

    private static void AppendToFormat(StringBuilder format, int argumentIndex, string placeholder)
    {
        var suffix = placeholder.IndexOfAny([',', ':']);
        format.Append('{').Append(argumentIndex);

        if (suffix >= 0)
        {
            format.Append(placeholder.Substring(suffix));
        }

        format.Append('}');
    }

    private static bool TryFormatting(string format, object[] values, out string value)
    {
        value = null;

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
}
