using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

/// <summary>
/// Renders a log message as written, so its length can be measured.
/// <para>
/// Only compile-time constant strings are read: literals, and <c>const</c> fields and locals (so centralised messages, e.g.
/// for translation, are still checked). Anything else, whether it's the whole message or only part of it, is given a nominal
/// length of <see cref="LogMessagesMinimumLengthAnalyzer.NominalParameterLength"/>, as its value isn't known.
/// </para>
/// <para>
/// The same applies to the values substituted into a message template, an interpolated string, or a <c>string.Format</c> call.
/// The rendered text is only ever measured.
/// </para>
/// </summary>
internal sealed class LogMessageRenderer(SemanticModel semanticModel, CancellationToken cancellationToken)
{
    /// <summary>
    /// Stands in for a value that isn't a constant string. It isn't whitespace, so it still counts when at either end of a
    /// trimmed message, and it isn't a brace, so it can't be mistaken for part of a template placeholder.
    /// </summary>
    private static readonly string Nominal = new('_', LogMessagesMinimumLengthAnalyzer.NominalParameterLength);

    /// <summary>
    /// The operation kind used by C# 12+ collection expressions, including implicitly for C# 13 params collections.
    /// It is looked up by name because this compiles against a Roslyn version that predates it; on older hosts it is unavailable.
    /// </summary>
    private static readonly OperationKind? CollectionExpressionKind =
        Enum.TryParse<OperationKind>("CollectionExpression", out var kind) ? kind : null;

    /// <summary>
    /// Renders the message passed to an <c>ILogger</c> method, substituting its template values.
    /// </summary>
    public string RenderMessage(InvocationExpressionSyntax invocation, IMethodSymbol method, ExpressionSyntax message)
    {
        var text = Render(message);

        if (text.IndexOf('{') < 0 || !TryGettingTemplateValues(invocation, method, out var values))
        {
            return text; // Without values, the logger doesn't format the template, so it's logged as written.
        }

        return RenderTemplate(text, (position, _, formatSuffix) =>
            Align(position < values.Count ? values[position] ?? Nominal : Nominal, formatSuffix));
    }

    /// <summary>
    /// Renders a message template whose values are only supplied at runtime (e.g. a <c>LoggerMessage.Define</c> format string).
    /// </summary>
    public string RenderTemplateWithoutValues(ExpressionSyntax template)
    {
        return TemplateWithNominalParameters(Render(template));
    }

    /// <summary>
    /// Renders a message template whose values are only supplied at runtime (e.g. a <c>[LoggerMessage]</c> attribute's message),
    /// giving each placeholder the nominal length.
    /// </summary>
    public static string TemplateWithNominalParameters(string template)
    {
        return RenderTemplate(template, (_, _, formatSuffix) => Align(Nominal, formatSuffix));
    }

    /// <summary>
    /// Renders a message template or format string as it's formatted: escaped braces become a single brace, and each
    /// placeholder is replaced by <paramref name="renderPlaceholder"/>, given the placeholder's position, its name (or index),
    /// and its alignment and/or format specifier (e.g. <c>",10:N2"</c>, or empty). An unmatched <c>{</c> or <c>}</c> is
    /// measured as written.
    /// </summary>
    private static string RenderTemplate(string template, Func<int, string, string, string> renderPlaceholder)
    {
        var result = new StringBuilder(template.Length);
        var position = 0;

        for (var index = 0; index < template.Length; index++)
        {
            var character = template[index];

            if ((character == '{' || character == '}') && index + 1 < template.Length && template[index + 1] == character)
            {
                result.Append(character);
                index++;
                continue;
            }

            var end = character == '{' ? template.IndexOf('}', index + 1) : -1;

            if (end < 0)
            {
                result.Append(character);
                continue;
            }

            var placeholder = template.Substring(index + 1, end - index - 1);
            var suffixStart = placeholder.IndexOfAny([',', ':']);
            result.Append(renderPlaceholder(
                position++,
                suffixStart < 0 ? placeholder : placeholder.Substring(0, suffixStart),
                suffixStart < 0 ? string.Empty : placeholder.Substring(suffixStart)));
            index = end;
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the template's values in order: each is its text if it's a constant string, otherwise <see langword="null"/>.
    /// </summary>
    /// <returns><see langword="false"/> if no values are passed, so the template isn't formatted.</returns>
    private bool TryGettingTemplateValues(InvocationExpressionSyntax invocation, IMethodSymbol method, out List<string> values)
    {
        values = [];
        var parameters = method.Parameters;

        if (parameters.Length == 0 || !parameters[parameters.Length - 1].IsParams)
        {
            return false;
        }

        var paramsIndex = parameters.Length - 1;
        var arguments = invocation.ArgumentList.Arguments;

        // Named arguments, or the params array passed itself, can't be matched by position, so are left to the operation.
        if (arguments.Any(argument => argument.NameColon != null)
            || (arguments.Count == paramsIndex + 1
                && semanticModel.GetTypeInfo(arguments[paramsIndex].Expression, cancellationToken).Type is IArrayTypeSymbol))
        {
            return TryGettingTemplateValuesFromOperation(invocation, values);
        }

        for (var index = paramsIndex; index < arguments.Count; index++)
        {
            values.Add(GetConstantString(semanticModel.GetConstantValue(arguments[index].Expression, cancellationToken)));
        }

        return values.Count > 0;
    }

    private bool TryGettingTemplateValuesFromOperation(InvocationExpressionSyntax invocation, List<string> values)
    {
        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation call)
        {
            return true; // The values can't be found, so each placeholder is given the nominal length.
        }

        var paramsArgument = call.Arguments.FirstOrDefault(argument => argument.Parameter?.IsParams == true);

        if (paramsArgument == null)
        {
            return false;
        }

        if (!TryGettingParamsElements(paramsArgument.Value, out var elements))
        {
            return true; // The values are passed as an array variable, so each placeholder is given the nominal length.
        }

        values.AddRange(elements.Select(element => GetConstantString(Unwrap(element)?.ConstantValue ?? default)));
        return values.Count > 0;
    }

    /// <summary>
    /// Gets a constant string's value, or <see langword="null"/> if it isn't a constant string. A <see langword="null"/>
    /// constant isn't a string, so it's given the nominal length like any other value.
    /// </summary>
    private static string GetConstantString(Optional<object> constant)
    {
        return constant.HasValue ? constant.Value as string : null;
    }

    private string Render(ExpressionSyntax expression)
    {
        // Most messages are constants, which are read without getting the operation, as the first operation
        // requested in a method builds the whole method's operation tree.
        return GetConstantString(semanticModel.GetConstantValue(expression, cancellationToken))
            ?? Render(semanticModel.GetOperation(expression, cancellationToken));
    }

    /// <summary>
    /// Renders any part of a message: constant strings as their text, supported string building recursively,
    /// and anything else as the nominal value.
    /// </summary>
    private string Render(IOperation operation)
    {
        cancellationToken.ThrowIfCancellationRequested();
        operation = Unwrap(operation);

        if (operation == null)
        {
            return Nominal;
        }

        if (GetConstantString(operation.ConstantValue) is { } text)
        {
            return text;
        }

        if (operation is IInterpolatedStringOperation interpolation)
        {
            return RenderInterpolation(interpolation);
        }

        if (IsStringConcatenation(operation))
        {
            return RenderConcatenation((IBinaryOperation)operation);
        }

        if (operation is IInvocationOperation call && IsSupportedStringMethod(call)
            && TryRenderingStringMethod(call, out var rendered))
        {
            return rendered;
        }

        return Nominal;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion:
                    operation = conversion.Operand;
                    break;
                case IParenthesizedOperation parentheses:
                    operation = parentheses.Operand;
                    break;
                default:
                    return operation;
            }
        }
    }

    /// <summary>
    /// Checks for a built-in string concatenation, e.g. <c>"Failed: " + id</c>.
    /// </summary>
    private static bool IsStringConcatenation(IOperation operation)
    {
        return operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Add, OperatorMethod: null } binary
            && binary.Type?.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    /// Checks for a call to <c>string.Concat</c>, <c>string.Format</c> or <c>string.Join</c>, the methods messages are built with.
    /// </summary>
    private static bool IsSupportedStringMethod(IInvocationOperation call)
    {
        return call.TargetMethod.ContainingType.SpecialType == SpecialType.System_String
            && call.TargetMethod.IsStatic
            && call.TargetMethod.Name is nameof(string.Concat) or nameof(string.Format) or nameof(string.Join);
    }

    /// <summary>
    /// Renders a concatenation such as <c>"Failed: " + a + b</c>, following the chain of left operands with a loop rather
    /// than recursion, so a long concatenation can't overflow the call stack.
    /// </summary>
    private string RenderConcatenation(IBinaryOperation concatenation)
    {
        var rightOperands = new Stack<IOperation>();
        IOperation left = concatenation;

        while (IsStringConcatenation(left))
        {
            var binary = (IBinaryOperation)left;
            rightOperands.Push(binary.RightOperand);
            left = Unwrap(binary.LeftOperand);
        }

        var result = new StringBuilder(Render(left));

        while (rightOperands.Count > 0)
        {
            result.Append(Render(rightOperands.Pop()));
        }

        return result.ToString();
    }

    private string RenderInterpolation(IInterpolatedStringOperation interpolation)
    {
        var result = new StringBuilder();

        foreach (var part in interpolation.Parts)
        {
            switch (part)
            {
                case IInterpolatedStringTextOperation literal:
                    result.Append(literal.Text.ConstantValue.Value as string);
                    break;
                case IInterpolationOperation hole:
                    // Format specifiers don't apply to strings, or to values that aren't known, but a constant alignment pads either.
                    var value = Render(hole.Expression);
                    result.Append(hole.Alignment?.ConstantValue is { HasValue: true, Value: int alignment } ? Align(value, alignment) : value);
                    break;
                default:
                    result.Append(Nominal);
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Renders <c>string.Concat</c>, <c>string.Format</c> and <c>string.Join</c> calls whose arguments are written inline.
    /// </summary>
    private bool TryRenderingStringMethod(IInvocationOperation call, out string rendered)
    {
        rendered = null;

        return call.TargetMethod.Name switch
        {
            nameof(string.Concat) => TryRenderingConcat(call, out rendered),
            nameof(string.Format) => TryRenderingFormat(call, out rendered),
            nameof(string.Join) => TryRenderingJoin(call, out rendered),
            _ => false,
        };
    }

    private bool TryRenderingConcat(IInvocationOperation call, out string rendered)
    {
        rendered = null;

        if (!TryGettingInlineValues(call.Arguments, out var values))
        {
            return false;
        }

        rendered = string.Concat(values.Select(Render));
        return true;
    }

    private bool TryRenderingJoin(IInvocationOperation call, out string rendered)
    {
        rendered = null;
        var arguments = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal).ToList();

        // Only joins of values written inline can be rendered, e.g. string.Join(", ", "Failed", id), not joins of a collection
        if (arguments.Count != 2 || arguments[1].Parameter?.IsParams != true
            || !TryGettingInlineValues(arguments.Skip(1), out var values))
        {
            return false;
        }

        rendered = string.Join(Render(arguments[0].Value), values.Select(Render));
        return true;
    }

    /// <summary>
    /// Renders a <c>string.Format</c> call with a constant format string by replacing each format item with its argument,
    /// so <c>string.Format("Failed {0}", id)</c> is rendered as <c>Failed </c> plus the nominal value.
    /// Any format item that can't be matched to an argument is given the nominal value, as with a message template's placeholders,
    /// so the format string's text is always measured. This includes arguments passed as an array variable, and an index that's
    /// out of range or isn't a number.
    /// </summary>
    private bool TryRenderingFormat(IInvocationOperation call, out string rendered)
    {
        rendered = null;
        var arguments = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal).ToList();
        var formatIndex = arguments.FindIndex(argument => argument.Parameter?.Name == "format");

        if (formatIndex < 0
            || GetConstantString(Unwrap(arguments[formatIndex].Value).ConstantValue) is not { } format)
        {
            return false; // The format string isn't a constant, so the call can't be rendered.
        }

        // The values aren't known if they're passed as an array variable, so each format item is given the nominal value.
        var values = TryGettingInlineValues(arguments.Skip(formatIndex + 1), out var inlineValues) ? inlineValues : [];

        rendered = RenderTemplate(format, (_, name, formatSuffix) => Align(RenderFormatValue(name, values), formatSuffix));
        return true;
    }

    private string RenderFormatValue(string indexText, List<IOperation> values)
    {
        return int.TryParse(indexText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < values.Count
            ? Render(values[index])
            : Nominal;
    }

    /// <summary>
    /// Gets argument values in parameter order, expanding params arguments written inline (an array or collection expression).
    /// Fails if a params argument is passed as a variable, as the number of values isn't known.
    /// </summary>
    private static bool TryGettingInlineValues(IEnumerable<IArgumentOperation> arguments, out List<IOperation> values)
    {
        values = [];

        foreach (var argument in arguments.OrderBy(argument => argument.Parameter?.Ordinal))
        {
            if (argument.Parameter == null)
            {
                return false;
            }

            if (!argument.Parameter.IsParams)
            {
                values.Add(argument.Value);
            }
            else if (TryGettingParamsElements(argument.Value, out var elements))
            {
                values.AddRange(elements);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the elements passed to a params parameter, whether as an inline array or as a params collection.
    /// </summary>
    private static bool TryGettingParamsElements(IOperation value, out IEnumerable<IOperation> elements)
    {
        elements = null;

        if (value is IArrayCreationOperation { Initializer: not null } array)
        {
            elements = array.Initializer.ElementValues;
            return true;
        }

        if (CollectionExpressionKind.HasValue && value?.Kind == CollectionExpressionKind.Value)
        {
            // A collection expression's child operations are its elements; spread elements are given the nominal length.
            elements = value.ChildOperations;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Applies the alignment in a format suffix (e.g. <c>",10:N2"</c>, or empty), ignoring any format specifier,
    /// as format specifiers don't apply to strings, or to values that aren't known.
    /// </summary>
    private static string Align(string value, string formatSuffix)
    {
        if (!formatSuffix.StartsWith(",", StringComparison.Ordinal))
        {
            return value;
        }

        var colon = formatSuffix.IndexOf(':');
        var alignmentText = colon < 0 ? formatSuffix.Substring(1) : formatSuffix.Substring(1, colon - 1);

        return int.TryParse(alignmentText.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var alignment)
            ? Align(value, alignment)
            : value;
    }

    /// <summary>
    /// Pads a value to an alignment's width, on the left if positive or the right if negative. The padding is capped at
    /// <see cref="LogMessagesMinimumLengthAnalyzer.MaximumSettingValue"/> so it can't allocate excessive padding; this doesn't
    /// change whether a message is long enough, as padding at either end is trimmed either way.
    /// </summary>
    private static string Align(string value, int alignment)
    {
        var width = (int)Math.Min(Math.Abs((long)alignment), LogMessagesMinimumLengthAnalyzer.MaximumSettingValue + (long)value.Length);
        return alignment < 0 ? value.PadRight(width) : value.PadLeft(width);
    }
}
