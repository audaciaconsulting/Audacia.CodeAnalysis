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
    internal const int MaximumSettingValue = 100;
    public const int DefaultLogMessageMinimumLength = 10;

    private const string LoggerMessageParameterName = "message";

    private const string LoggerMessageTypeName = "Microsoft.Extensions.Logging.LoggerMessage";
    private const string LoggerMessageDefineMethodName = "Define";
    private const string LoggerMessageDefineParameterName = "formatString";

    private const string LoggerMessageAttributeTypeName = "Microsoft.Extensions.Logging.LoggerMessageAttribute";
    private const string LoggerMessageAttributeMessageProperty = "Message";

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

        var loggerMessageType = startContext.Compilation.GetTypeByMetadataName(LoggerMessageTypeName);
        var loggerMessageAttributeType = startContext.Compilation.GetTypeByMetadataName(LoggerMessageAttributeTypeName);

        startContext.RegisterSyntaxNodeAction(
            nodeAnalysisContext => AnalyzeInvocation(nodeAnalysisContext, settingsReader, loggerMessageType),
            SyntaxKind.InvocationExpression);

        // [LoggerMessage] only exists in Microsoft.Extensions.Logging.Abstractions 6.0+, so there's nothing to check without it
        if (loggerMessageAttributeType != null)
        {
            startContext.RegisterSymbolAction(
                symbolAnalysisContext => AnalyzeLoggerMessageAttribute(symbolAnalysisContext, settingsReader, loggerMessageAttributeType),
                SymbolKind.Method);
        }
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext nodeAnalysisContext,
        ISettingsReader settingsReader,
        INamedTypeSymbol loggerMessageType)
    {
        var invocation = (InvocationExpressionSyntax)nodeAnalysisContext.Node;

        if (IsLoggerMessageDefineCall(invocation)
            && TryAnalyzingLoggerMessageDefine(nodeAnalysisContext, settingsReader, loggerMessageType))
        {
            return;
        }

        if (!invocation.TryGetLoggerMethodSymbol(nodeAnalysisContext.SemanticModel, out IMethodSymbol methodSymbol))
        {
            return;
        }

        if (!invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageParameterName, out ArgumentSyntax messageArgument))
        {
            return;
        }

        TryAnalyzingILoggerMessage(nodeAnalysisContext, settingsReader, invocation, messageArgument);
    }

    /// <summary>
    /// A cheap syntactic check for <c>LoggerMessage.Define(...)</c> and <c>LoggerMessage.Define&lt;T&gt;(...)</c>,
    /// so the semantic model is only consulted for likely candidates.
    /// </summary>
    private static bool IsLoggerMessageDefineCall(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == LoggerMessageDefineMethodName,
            SimpleNameSyntax name => name.Identifier.ValueText == LoggerMessageDefineMethodName,
            _ => false,
        };
    }

    /// <summary>
    /// Checks the format string passed to <c>LoggerMessage.Define</c>. The template is measured as written,
    /// as the values are only supplied when the delegate is invoked.
    /// </summary>
    /// <returns><see langword="false"/> if the call isn't to <c>LoggerMessage.Define</c>, so other checks can still run.</returns>
    private static bool TryAnalyzingLoggerMessageDefine(
        SyntaxNodeAnalysisContext nodeAnalysisContext,
        ISettingsReader settingsReader,
        INamedTypeSymbol loggerMessageType)
    {
        var invocation = (InvocationExpressionSyntax)nodeAnalysisContext.Node;

        if (loggerMessageType == null
            || nodeAnalysisContext.SemanticModel.GetSymbolInfo(invocation, nodeAnalysisContext.CancellationToken).Symbol is not IMethodSymbol methodSymbol
            || !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, loggerMessageType))
        {
            return false;
        }

        var resolver = new LogMessageValueResolver(nodeAnalysisContext.SemanticModel, nodeAnalysisContext.CancellationToken);

        if (invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageDefineParameterName, out var formatArgument)
            && resolver.TryResolvingTemplateAsWritten(formatArgument.Expression, out var formatString))
        {
            ReportIfTooShort(nodeAnalysisContext.ReportDiagnostic, settingsReader, formatString, formatArgument.GetLocation());
        }

        return true;
    }

    /// <summary>
    /// Checks the message of a source-generated <c>[LoggerMessage]</c> method. Attribute arguments are always
    /// compile-time constants, so the message template is measured as written. Attributes without a message are skipped.
    /// </summary>
    private static void AnalyzeLoggerMessageAttribute(
        SymbolAnalysisContext symbolAnalysisContext,
        ISettingsReader settingsReader,
        INamedTypeSymbol loggerMessageAttributeType)
    {
        foreach (var attribute in symbolAnalysisContext.Symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, loggerMessageAttributeType)
                || !TryGetAttributeMessage(attribute, out var message, out var constructorIndex)
                || attribute.ApplicationSyntaxReference?.GetSyntax(symbolAnalysisContext.CancellationToken) is not AttributeSyntax attributeSyntax)
            {
                continue;
            }

            var location = GetAttributeMessageLocation(attributeSyntax, constructorIndex);
            ReportIfTooShort(symbolAnalysisContext.ReportDiagnostic, settingsReader, LogMessageAsWrittenRenderer.TemplateAsWritten(message), location);
        }
    }

    /// <summary>
    /// Gets the message from either the <c>Message</c> property or the constructor's <c>message</c> parameter.
    /// </summary>
    /// <param name="constructorIndex">The index of the constructor's message parameter, or -1 if it was set via the property.</param>
    private static bool TryGetAttributeMessage(AttributeData attribute, out string message, out int constructorIndex)
    {
        message = null;
        constructorIndex = -1;

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == LoggerMessageAttributeMessageProperty)
            {
                message = namedArgument.Value.Value as string;
                return message != null;
            }
        }

        var parameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;

        for (var index = 0; index < parameters.Length && index < attribute.ConstructorArguments.Length; index++)
        {
            if (parameters[index].Name == LoggerMessageParameterName)
            {
                message = attribute.ConstructorArguments[index].Value as string;
                constructorIndex = index;
                return message != null;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the attribute argument holding the message, falling back to the whole attribute if it can't be matched.
    /// </summary>
    private static Location GetAttributeMessageLocation(AttributeSyntax attributeSyntax, int constructorIndex)
    {
        var arguments = attributeSyntax.ArgumentList?.Arguments ?? default;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];

            var isMessageArgument = constructorIndex < 0
                ? argument.NameEquals?.Name.Identifier.ValueText == LoggerMessageAttributeMessageProperty
                : IsMessageArgument(constructorIndex, index, argument);

            if (isMessageArgument)
            {
                return argument.GetLocation();
            }
        }

        return attributeSyntax.GetLocation();
    }

    private static bool IsMessageArgument(int constructorIndex, int index, AttributeArgumentSyntax argument)
    {
        return argument.NameColon?.Name.Identifier.ValueText == LoggerMessageParameterName
            || (argument.NameColon == null && argument.NameEquals == null && index == constructorIndex);
    }

    private static void TryAnalyzingILoggerMessage(SyntaxNodeAnalysisContext nodeAnalysisContext, ISettingsReader settingsReader, 
        InvocationExpressionSyntax invocation, ArgumentSyntax messageArgument)
    {
        var resolver = new LogMessageValueResolver(nodeAnalysisContext.SemanticModel, nodeAnalysisContext.CancellationToken);

        if (!resolver.TryResolvingMessage(invocation, messageArgument.Expression, out var messageParameterValue))
        {
            return;
        }

        ReportIfTooShort(nodeAnalysisContext.ReportDiagnostic, settingsReader, messageParameterValue, messageArgument.GetLocation());
    }

    private static void ReportIfTooShort(Action<Diagnostic> reportDiagnostic, ISettingsReader settingsReader, string message, Location location)
    {
        // Settings are read per syntax tree, as .editorconfig sections can apply different values to different files
        var minimumLength = GetMinimumLengthFromSettings(settingsReader, location.SourceTree)
            ?? DefaultLogMessageMinimumLength;

        if (message.Trim().Length < minimumLength)
        {
            reportDiagnostic(Diagnostic.Create(Rule, location, minimumLength));
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
