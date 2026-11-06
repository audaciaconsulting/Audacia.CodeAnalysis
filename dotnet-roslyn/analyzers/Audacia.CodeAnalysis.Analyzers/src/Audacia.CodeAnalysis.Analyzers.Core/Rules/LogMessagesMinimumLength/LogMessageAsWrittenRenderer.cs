using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

/// <summary>
/// Produces the 'as written' form of a log message, used when its exact value can't be determined at compile time.
/// <para>
/// Messages built from strings are measured as if written as an interpolated string, with each unknown value shown as
/// <c>{its source text}</c>, imitating a message template's <c>{Placeholder}</c> names.
/// </para>
/// <para>
/// The rendered text is only ever measured; it is never used as a value elsewhere (e.g. in locals or template arguments).
/// </para>
/// </summary>
internal sealed class LogMessageAsWrittenRenderer(LogMessageValueResolver resolver)
{
    /// <summary>
    /// The form of a message template measured when its argument/variable values aren't all known.
    /// </summary>
    public static string TemplateAsWritten(string template) => template;

    /// <summary>
    /// The form of an unknown value within a message built from strings, matching a template placeholder,
    /// e.g. <c>"Failed: " + ex.Message</c> is measured as <c>Failed: {ex.Message}</c>. Whitespace is collapsed.
    /// </summary>
    public static string Placeholder(string sourceText)
    {
        var placeholder = new StringBuilder("{");
        var previousWasWhitespace = false;

        foreach (var character in sourceText.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                previousWasWhitespace = true;
                continue;
            }

            if (previousWasWhitespace)
            {
                placeholder.Append(' ');
                previousWasWhitespace = false;
            }

            placeholder.Append(character);
        }

        return placeholder.Append('}').ToString();
    }

    /// <summary>
    /// Renders a message built from strings (an interpolated string, concatenation, or a <c>string.Concat</c>,
    /// <c>string.Format</c> or <c>string.Join</c> call) as written.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the message isn't built from strings (e.g. it's only a variable), or contains no known
    /// text, as there's nothing meaningful to measure; the rule is then skipped.
    /// </returns>
    public bool TryRendering(IOperation message, out string text)
    {
        text = null;
        message = Unwrap(message);

        if (!IsBuiltFromStrings(message))
        {
            return false; // A message that's only stored in a variable (or returned from a method) is skipped.
        }

        var hasKnownText = false;
        var rendered = Render(message, ref hasKnownText);

        if (!hasKnownText || resolver.WorkLimitExceeded)
        {
            return false; // The message is made up entirely of unknown values, so it's effectively just variables.
        }

        text = rendered;
        return true;
    }

    private static bool IsBuiltFromStrings(IOperation operation)
    {
        return operation is IInterpolatedStringOperation
            || LogMessageValueResolver.IsStringConcatenation(operation)
            || LogMessageValueResolver.IsSupportedStringMethod(operation);
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
    /// Renders any part of a message: known values as their text, supported string building recursively,
    /// and anything else as a placeholder of its source text.
    /// </summary>
    private string Render(IOperation operation, ref bool hasKnownText)
    {
        operation = Unwrap(operation);

        if (resolver.TryResolvingOperation(operation, out var known))
        {
            hasKnownText |= !string.IsNullOrWhiteSpace(known);
            return known;
        }

        if (resolver.WorkLimitExceeded)
        {
            // Stop rendering, as the message is skipped anyway. Each call uses at least one resolution step,
            // so this also bounds the recursion, e.g. for a long concatenation such as "Failed" + a + b + ... + z.
            return string.Empty;
        }

        if (operation is IInterpolatedStringOperation interpolation)
        {
            return RenderInterpolation(interpolation, ref hasKnownText);
        }

        if (LogMessageValueResolver.IsStringConcatenation(operation))
        {
            var binary = (IBinaryOperation)operation;
            return Render(binary.LeftOperand, ref hasKnownText) + Render(binary.RightOperand, ref hasKnownText);
        }

        if (LogMessageValueResolver.IsSupportedStringMethod(operation)
            && TryRenderingStringMethod((IInvocationOperation)operation, ref hasKnownText, out var rendered))
        {
            return rendered;
        }

        return Placeholder(operation.Syntax.ToString());
    }

    private string RenderInterpolation(IInterpolatedStringOperation interpolation, ref bool hasKnownText)
    {
        var result = new StringBuilder();

        foreach (var part in interpolation.Parts)
        {
            if (resolver.TryResolvingInterpolationPart(part, out var known))
            {
                hasKnownText |= !string.IsNullOrWhiteSpace(known);
                result.Append(known);
            }
            else if (part.Syntax is InterpolationSyntax hole)
            {
                // Keep any alignment and format specifier, as a template placeholder would
                result.Append(Placeholder(hole.Expression.ToString() + hole.AlignmentClause + hole.FormatClause));
            }
            else
            {
                result.Append(Placeholder(part.Syntax.ToString()));
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Renders <c>string.Concat</c>, <c>string.Format</c> and <c>string.Join</c> calls whose arguments are written inline.
    /// </summary>
    private bool TryRenderingStringMethod(IInvocationOperation call, ref bool hasKnownText, out string rendered)
    {
        rendered = null;

        return call.TargetMethod.Name switch
        {
            nameof(string.Concat) => TryRenderingConcat(call, ref hasKnownText, out rendered),
            nameof(string.Format) => TryRenderingFormat(call, ref hasKnownText, out rendered),
            nameof(string.Join) => TryRenderingJoin(call, ref hasKnownText, out rendered),
            _ => false,
        };
    }

    private bool TryRenderingConcat(IInvocationOperation call, ref bool hasKnownText, out string rendered)
    {
        rendered = null;

        if (!LogMessageValueResolver.TryGettingInlineValues(call.Arguments, out var values))
        {
            return false;
        }

        var result = new StringBuilder();

        foreach (var value in values)
        {
            result.Append(Render(value, ref hasKnownText));
        }

        rendered = result.ToString();
        return true;
    }

    private bool TryRenderingJoin(IInvocationOperation call, ref bool hasKnownText, out string rendered)
    {
        rendered = null;
        var arguments = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal).ToList();

        // Only joins of values written inline can be rendered, e.g. string.Join(", ", "Failed", id), not joins of a collection
        if (arguments.Count != 2 || arguments[1].Parameter?.IsParams != true
            || !LogMessageValueResolver.TryGettingInlineValues(arguments.Skip(1), out var values))
        {
            return false;
        }

        var result = new StringBuilder();

        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                result.Append(Render(arguments[0].Value, ref hasKnownText));
            }

            result.Append(Render(values[index], ref hasKnownText));
        }

        rendered = result.ToString();
        return true;
    }

    /// <summary>
    /// Renders a <c>string.Format</c> call by replacing each format item with its argument, so
    /// <c>string.Format("Failed {0}", id)</c> is rendered as <c>Failed {id}</c>, as an interpolated string would be.
    /// </summary>
    private bool TryRenderingFormat(IInvocationOperation call, ref bool hasKnownText, out string rendered)
    {
        rendered = null;
        var arguments = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal).ToList();
        var formatIndex = arguments.FindIndex(argument => argument.Parameter?.Name == "format");

        if (formatIndex < 0
            || !resolver.TryResolvingOperation(arguments[formatIndex].Value, out var format)
            || !LogMessageValueResolver.TryGettingInlineValues(arguments.Skip(formatIndex + 1), out var values))
        {
            return false; // The format string or its arguments aren't written inline, so the call can't be rendered.
        }

        var result = new StringBuilder();

        for (var index = 0; index < format.Length; index++)
        {
            var character = format[index];

            if ((character == '{' || character == '}') && index + 1 < format.Length && format[index + 1] == character)
            {
                hasKnownText = true;
                result.Append(character).Append(character); // Escaped braces are kept as written, as in a template
                index++;
                continue;
            }

            if (character != '{')
            {
                hasKnownText |= !char.IsWhiteSpace(character);
                result.Append(character);
                continue;
            }

            var end = format.IndexOf('}', index + 1);

            if (end < 0 || !TryRenderingFormatItem(format.Substring(index + 1, end - index - 1), values, ref hasKnownText, out var item))
            {
                return false; // The format string isn't valid, so the call can't be rendered.
            }

            result.Append(item);
            index = end;
        }

        rendered = result.ToString();
        return true;
    }

    private bool TryRenderingFormatItem(string formatItem, IReadOnlyList<IOperation> values, ref bool hasKnownText, out string rendered)
    {
        rendered = null;
        var suffixStart = formatItem.IndexOfAny([',', ':']);
        var indexText = suffixStart < 0 ? formatItem : formatItem.Substring(0, suffixStart);

        if (!int.TryParse(indexText.Trim(), out var argumentIndex) || argumentIndex < 0 || argumentIndex >= values.Count)
        {
            return false;
        }

        var value = values[argumentIndex];

        if (suffixStart < 0)
        {
            rendered = Render(value, ref hasKnownText);
            return true;
        }

        // Alignment and format specifiers can't be applied reliably, so keep them on the placeholder
        rendered = Placeholder(value.Syntax.ToString() + formatItem.Substring(suffixStart));
        return true;
    }
}
