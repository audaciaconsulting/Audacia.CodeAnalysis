using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        // Settings are read per syntax tree, as .editorconfig sections can apply different values to different files
        var minimumLength = GetMinimumLengthFromSettings(settingsReader, invocation.SyntaxTree)
            ?? DefaultLogMessageMinimumLength;

        if (messageParameterValue.Length < minimumLength)
        {
            var diagnostic = Diagnostic.Create(Rule, messageArgument.GetLocation(), minimumLength);
            nodeAnalysisContext.ReportDiagnostic(diagnostic);
        }
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
