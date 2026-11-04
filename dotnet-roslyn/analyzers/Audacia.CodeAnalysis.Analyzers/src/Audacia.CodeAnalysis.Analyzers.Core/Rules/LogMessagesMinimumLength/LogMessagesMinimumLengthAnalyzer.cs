using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LogMessagesMinimumLengthAnalyzer : DiagnosticAnalyzer
{
    private readonly ISettingsReader _settingsReader;

    public const string Id = DiagnosticId.LogMessageMinimumLength;

    public const string LogMessageMinimumLengthSetting = "log_message_minimum_length";
    private const int MinimumSettingValue = 1;
    private const int MaximumSettingValue = 50;
    public const int DefaultLogMessageMinimumLength = 10;

    private const string LoggerMessageParameterName = "message";

    private const string Title = "Log message is too short";
    private const string MessageFormat = "Log messages should be at least {0} characters long";
    private const string Description = "Log messages should be long enough to be meaningful.";

    private const string Category = DiagnosticCategory.Usage;
    private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        Description,
        HelpLinkUrl);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public LogMessagesMinimumLengthAnalyzer()
    {
    }

    public LogMessagesMinimumLengthAnalyzer(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(RegisterCompilationStart);
    }

    private void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
    {
        // Use the injected ISettingsReader if there is one, as that means we're in a unit test
        var settingsReader = _settingsReader ?? new EditorConfigSettingsReader(startContext.Options);

        startContext.RegisterSyntaxNodeAction(
            nodeAnalysisContext => AnalyzeInvocation(nodeAnalysisContext, settingsReader),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext nodeAnalysisContext, ISettingsReader settingsReader)
    {
        var invocation = (InvocationExpressionSyntax)nodeAnalysisContext.Node;

        if (!invocation.TryGetLoggerMethodSymbol(nodeAnalysisContext.SemanticModel, out IMethodSymbol methodSymbol))
        {
            return;
        }

        if (!invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageParameterName, out ArgumentSyntax messageArgument))
        {
            return;
        }

        var messageExpression = messageArgument.Expression.TryResolveToStringLiteral(nodeAnalysisContext.SemanticModel, out var resolvedExpression)
                ? resolvedExpression
                : messageArgument.Expression;

        string messageParameterValue = messageArgument.Expression.ToString(); // This will be the string literal (ideally) or interpolated string literal, including quotes and braces
        messageParameterValue = messageParameterValue.Trim('"').Trim(); // Remove quotes and spaces from string literal

        if (messageExpression is not LiteralExpressionSyntax
            && messageExpression is not InterpolatedStringExpressionSyntax
            && messageParameterValue.Contains("+"))
        {
            messageParameterValue = Regex.Replace(messageParameterValue, @"""\s*[+]\s*""", string.Empty);
        }

        if (TryEvaluateStringFormat(messageExpression, nodeAnalysisContext, out var formattedMessage))
        {
            messageParameterValue = formattedMessage;
        }

        // Settings are read per syntax tree, as .editorconfig sections can apply different values to different files
        var minimumLength = GetMinimumLengthFromSettings(settingsReader, invocation.SyntaxTree)
            ?? DefaultLogMessageMinimumLength;

        if (messageParameterValue.Length < minimumLength)
        {
            var diagnostic = Diagnostic.Create(Rule, messageArgument.GetLocation(), minimumLength);
            nodeAnalysisContext.ReportDiagnostic(diagnostic);
        }
    }

    private static bool TryEvaluateStringFormat(
        ExpressionSyntax expression,
        SyntaxNodeAnalysisContext context,
        out string result)
    {
        result = null;

        if (context.SemanticModel.GetOperation(expression, context.CancellationToken) is not IInvocationOperation call)
        {
            return false;
        }

        var method = call.TargetMethod;

        if (method.ContainingType.SpecialType != SpecialType.System_String
            || method.Name != nameof(string.Format)
            || method.Parameters.Length < 2
            || method.Parameters[0].Type.SpecialType != SpecialType.System_String)
        {
            return false;
        }

        var hasParamsArray = GetHasParamsArray(method);
        var values = new List<object>();

        if (!hasParamsArray)
        {
            bool value = AddNullsForParams(method, values, out bool canContinue);
            if (!canContinue)
            {
                return value;
            }
        }

        string format = null;

        foreach (var argument in call.Arguments)
        {
            bool value = AddCallArguments(hasParamsArray, values, ref format, argument, out bool canContinue);
            if (!canContinue)
            {
                return value;
            }
        }

        if (format == null)
        {
            return false;
        }

        try
        {
            result = string.Format(format, values.ToArray());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool AddNullsForParams(IMethodSymbol method, List<object> values, out bool canContinue)
    {
        canContinue = false;

        for (var index = 1; index < method.Parameters.Length; index++)
        {
            if (method.Parameters[index].Type.SpecialType != SpecialType.System_Object)
            {
                return false;
            }

            values.Add(null);
        }

        canContinue = true;
        return true;
    }

    private static bool GetHasParamsArray(IMethodSymbol method)
    {
        return method.Parameters[1].IsParams
                    && method.Parameters[1].Type is IArrayTypeSymbol arrayType
                    && arrayType.ElementType.SpecialType == SpecialType.System_Object;
    }

    private static bool AddCallArguments(bool hasParamsArray, List<object> values, ref string format, IArgumentOperation argument, out bool canContinue)
    {
        canContinue = false;

        if (argument.Parameter == null)
        {
            return false;
        }

        if (argument.Parameter.Ordinal == 0)
        {
            if (!TryGetConstantString(argument.Value, out var formatValue) || formatValue is not string formatText)
            {
                return false;
            }

            format = formatText;
        }
        else if (hasParamsArray)
        {
            // Roslyn represents expanded params arguments as an implicit array creation. Explicit inline arrays can be read in exactly the same way.
            if (argument.Value is not IArrayCreationOperation array || array.Initializer == null)
            {
                return false;
            }

            foreach (var element in array.Initializer.ElementValues)
            {
                if (!TryGetConstantString(element, out var value))
                {
                    return false;
                }

                values.Add(value);
            }
        }
        else
        {
            if (!TryGetConstantString(argument.Value, out var value))
            {
                return false;
            }

            // Parameter ordinals preserve placeholder order for reordered named arguments.
            values[argument.Parameter.Ordinal - 1] = value;
        }

        canContinue = true;
        return true;
    }

    private static bool TryGetConstantString(IOperation operation, out object value)
    {
        // Boxing/reference conversions need not carry the operand's constant value.
        while (operation is IConversionOperation conversion && conversion.OperatorMethod == null)
        {
            operation = conversion.Operand;
        }

        var constant = operation.ConstantValue;
        value = constant.HasValue ? constant.Value : null;
        return constant.HasValue && (value == null || value is string);
    }

    private static int? GetMinimumLengthFromSettings(ISettingsReader settingsReader, SyntaxTree syntaxTree)
    {
        var key = new SettingsKey(Id, LogMessageMinimumLengthSetting);
        var minimumLength = settingsReader.TryGetInt(syntaxTree, key);

        if (minimumLength.HasValue)
        {
            if (minimumLength.Value < MinimumSettingValue)
            {
                throw new InvalidConfigException($"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be at least {MinimumSettingValue}.");
            }
            else if (minimumLength.Value > MaximumSettingValue)
            {
                throw new InvalidConfigException($"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be at most {MaximumSettingValue}.");
            }
        }

        return minimumLength;
    }
}
