using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.Logging.HandlerShouldInjectILogger
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HandlerShouldInjectILoggerAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.HandlerShouldInjectILogger;

        public const string HandlerEndingIdentifiersSettingKey = "handler_ending_term";

        private const string Title = "Command Handlers should inject ILogger instance";
        private const string MessageFormat = "Handler '{0}' should inject ILogger";
        private const string Description = "Inject ILogger instance into handler.";

        private const string ILoggerType = "ILogger";

        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor HandlerShouldInjectILoggerRule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        private const string Category = DiagnosticCategory.Observability;

        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        public static readonly IEnumerable<string> DefaultHandlerEndingIdentifiers = new List<string>()
        {
            "Handler",
            "Command"
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(HandlerShouldInjectILoggerRule);

        public HandlerShouldInjectILoggerAnalyzer()
        {
        }

        public HandlerShouldInjectILoggerAnalyzer(ISettingsReader settingsReader)
            : this()
        {
            _settingsReader = settingsReader;
            _isSettingsReaderInjected = true;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            // We don't want to reinitialize if the ISettingsReader object was injected as that means we're in a unit test
            if (_settingsReader == null || !_isSettingsReaderInjected)
            {
                _settingsReader = new EditorConfigSettingsReader(startContext.Options);
            }

            startContext.RegisterSymbolAction(
                actionContext => AnalyzeMethod(actionContext),
                SymbolKind.Method);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            var settings =
                GetInjectILoggerSettings(
                    method,
                    context.Symbol.DeclaringSyntaxReferences[0].SyntaxTree);

            var memberName = method.ContainingType.Name;

            if (!method.IsPropertyOrEventAccessor() &&
                MemberRequiresAnalysis(method, context.CancellationToken) &&
                method.IsConstructor() &&
                settings.HandlerEndingIdentifiers.Any(handlerEndingIdentifier =>
                    memberName.EndsWith(handlerEndingIdentifier)))
            {
                var handlerShouldInjectILoggerInfo = new HandlerShouldInjectILoggerInfo(method, settings);

                AnalyzeConstructorParameters(context, handlerShouldInjectILoggerInfo, memberName);
            }
        }

        private InjectILoggerSettings GetInjectILoggerSettings(
            IMethodSymbol symbol,
            SyntaxTree syntaxTree)
        {
            var handlerEndingIdentifiers = DefaultHandlerEndingIdentifiers;

            var rawHandlerEndingIdentifiers =
                _settingsReader.TryGetValue(syntaxTree, new SettingsKey(Id, HandlerEndingIdentifiersSettingKey));
            if (!string.IsNullOrEmpty(rawHandlerEndingIdentifiers))
            {
                handlerEndingIdentifiers = rawHandlerEndingIdentifiers.Split(',');
            }

            return new InjectILoggerSettings(handlerEndingIdentifiers);
        }

        private static bool MemberRequiresAnalysis(
            ISymbol member,
            CancellationToken cancellationToken)
        {
            return !member.IsExtern &&
                   !member.IsOverride &&
                   !member.HidesBaseMember(cancellationToken) &&
                   !member.IsRecordImplementation(cancellationToken) &&
                   !member.IsInterfaceImplementation();
        }

        private static void AnalyzeConstructorParameters(
            SymbolAnalysisContext context,
            HandlerShouldInjectILoggerInfo handlerShouldInjectILoggerInfo,
            string memberName)
        {
            var constructorParameters = handlerShouldInjectILoggerInfo.MethodSymbol.Parameters;

            var constructorHasILoggerInjected = constructorParameters.AsEnumerable()
                .Where(
                    parameter =>
                    {
                        var isILoggerType = parameter.Type.Name == ILoggerType;

                        return isILoggerType;
                    })
                .Any();

            if (!constructorHasILoggerInjected)
            {
                ReportParameterCount(context, handlerShouldInjectILoggerInfo, memberName);
            }
        }

        private static void ReportParameterCount(
            SymbolAnalysisContext context,
            HandlerShouldInjectILoggerInfo handlerShouldInjectILoggerInfo,
            string name)
        {
            if (!handlerShouldInjectILoggerInfo.MethodSymbol.IsSynthesized())
            {
                var diagnostic = Diagnostic.Create(
                    HandlerShouldInjectILoggerRule,
                    handlerShouldInjectILoggerInfo.MethodSymbol.Locations[0],
                    name,
                    handlerShouldInjectILoggerInfo.HandlerEndingTerms);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}