using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.Observability.HandlerShouldInjectILogger
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HandlerShouldInjectILoggerAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.HandlerShouldInjectILogger;

        public const string HandlerIdentifyingTermsSettingKey = "identifying";

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

        public static readonly IEnumerable<string> DefaultHandlerIdentifiersTerms = new List<string>()
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
                settings.HandlerIdentifyingTerms.Any(handlerIdentifyingTerm =>
                    memberName.EndsWith(handlerIdentifyingTerm)))
            {
                var handlerShouldInjectILoggerInfo = new HandlerShouldInjectILoggerInfo(method, settings);

                AnalyzeConstructorParameters(context, handlerShouldInjectILoggerInfo, memberName);
            }
        }

        private InjectILoggerSettings GetInjectILoggerSettings(
            IMethodSymbol symbol,
            SyntaxTree syntaxTree)
        {
            var handlerIdentifyingTerms = DefaultHandlerIdentifiersTerms;

            var rawHandlerIdentifyingTerms =
                _settingsReader.TryGetValue(syntaxTree, new SettingsKey(Id, HandlerIdentifyingTermsSettingKey));
            if (!string.IsNullOrEmpty(rawHandlerIdentifyingTerms))
            {
                handlerIdentifyingTerms = rawHandlerIdentifyingTerms.Split(',');
            }

            return new InjectILoggerSettings(handlerIdentifyingTerms);
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
                    handlerShouldInjectILoggerInfo.HandlerIdentifyingTerms);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}