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
    private readonly Action<string, Location> _onMessageRead;

    public const string Id = DiagnosticId.LogMessageMinimumLength;

    public const string LogMessageMinimumLengthSetting = "log_message_minimum_length";
    private const int MinimumSettingValue = 1;
    internal const int MaximumSettingValue = 100;
    public const int DefaultLogMessageMinimumLength = 10;

    /// <summary>
    /// The assumed length assigned to any 'unread' parameter or variable, e.g. in message ""Payment {paymentId} failed"
    /// , where paymentId is unknown at compile time, does paymentId count as 0 or 1?
    /// </summary>
    /// <remarks>
    /// <para>Only compile-time constant strings are 'read'; see <see cref="LogMessageRenderer"/>.</para>
    /// </remarks>
    public const byte NominalParameterLength = 0;

    private const string LoggerMessageParameterName = "message";

    private const string LoggerMessageTypeName = "Microsoft.Extensions.Logging.LoggerMessage";
    private const string LoggerMessageDefineMethodName = "Define";
    private const string LoggerMessageDefineParameterName = "formatString";

    private const string LoggerMessageAttributeTypeName = "Microsoft.Extensions.Logging.LoggerMessageAttribute";
    private const string LoggerMessageAttributeMessageProperty = "Message";

    private const string Title = "Log message is too short";
    private const string MessageFormat = "Log messages should be at least {0} characters long. LOG-01.4.";
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

    /// <summary>
    /// For unit tests: <paramref name="onMessageRead"/> is given each message exactly as it's measured (i.e. rendered and
    /// trimmed), whether or not it's too short, so tests can check the message was read as expected.
    /// </summary>
    /// <param name="settingsReader">The settings to use, or <see langword="null"/> to read them from <c>.editorconfig</c>.</param>
    /// <param name="onMessageRead">Called with each message as it's measured, and the location a diagnostic would be reported at. It may be called concurrently.</param>
    public LogMessagesMinimumLengthAnalyzer(ISettingsReader settingsReader, Action<string, Location> onMessageRead)
        : this(settingsReader)
    {
        _onMessageRead = onMessageRead;
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

    private void AnalyzeInvocation(
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

        AnalyzeILoggerMessage(nodeAnalysisContext, settingsReader, invocation, methodSymbol, messageArgument);
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
    /// Checks the format string passed to <c>LoggerMessage.Define</c>. Its placeholders are given a nominal length,
    /// as the values are only supplied when the delegate is invoked.
    /// </summary>
    /// <returns><see langword="false"/> if the call isn't to <c>LoggerMessage.Define</c>, so other checks can still run.</returns>
    private bool TryAnalyzingLoggerMessageDefine(
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

        if (invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageDefineParameterName, out var formatArgument))
        {
            var renderer = new LogMessageRenderer(nodeAnalysisContext.SemanticModel, nodeAnalysisContext.CancellationToken);
            var formatString = renderer.RenderTemplateWithoutValues(formatArgument.Expression);
            ReportIfTooShort(nodeAnalysisContext.ReportDiagnostic, settingsReader, formatString, formatArgument.GetLocation());
        }

        return true;
    }

    /// <summary>
    /// Checks the message of a source-generated <c>[LoggerMessage]</c> method. Attribute arguments are always
    /// compile-time constants, but the values aren't, so its placeholders are given a nominal length. Attributes without a message are skipped.
    /// </summary>
    private void AnalyzeLoggerMessageAttribute(
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
            ReportIfTooShort(symbolAnalysisContext.ReportDiagnostic, settingsReader, LogMessageRenderer.TemplateWithNominalParameters(message), location);
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

    private void AnalyzeILoggerMessage(SyntaxNodeAnalysisContext nodeAnalysisContext, ISettingsReader settingsReader,
        InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol, ArgumentSyntax messageArgument)
    {
        var renderer = new LogMessageRenderer(nodeAnalysisContext.SemanticModel, nodeAnalysisContext.CancellationToken);
        var message = renderer.RenderMessage(invocation, methodSymbol, messageArgument.Expression);
        ReportIfTooShort(nodeAnalysisContext.ReportDiagnostic, settingsReader, message, messageArgument.GetLocation());
    }

    private void ReportIfTooShort(Action<Diagnostic> reportDiagnostic, ISettingsReader settingsReader, string message, Location location)
    {
        // Settings are read per syntax tree, as .editorconfig sections can apply different values to different files
        var minimumLength = GetMinimumLengthFromSettings(settingsReader, location.SourceTree)
            ?? DefaultLogMessageMinimumLength;

        string trimmedMessage = message?.Trim() ?? string.Empty;
        _onMessageRead?.Invoke(trimmedMessage, location); // for unit tests to check the message was read as expected; ignored in production

        if (trimmedMessage.Length < minimumLength)
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
