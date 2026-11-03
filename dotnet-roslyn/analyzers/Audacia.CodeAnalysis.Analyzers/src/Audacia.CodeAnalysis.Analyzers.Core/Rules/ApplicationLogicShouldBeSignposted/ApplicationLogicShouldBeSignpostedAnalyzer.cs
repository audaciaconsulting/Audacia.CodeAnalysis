using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ApplicationLogicShouldBeSignposted
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ApplicationLogicShouldBeSignpostedAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.ApplicationLogicShouldBeSignposted;

        public const string HandlerSuffixConfigKey = "handler_suffix";

        private const string Title = "Application logic handler does not inject a logger";
        private const string MessageFormat = "Application logic handler '{0}' does not inject a logger. See Audacia standard LOG-04.6.";
        private const string Description = "Application logic handler should inject a logger to enable signposting of significant application events.";

        private const string LoggerMetadataName = "Microsoft.Extensions.Logging.ILogger";
        private const string LoggerFactoryMetadataName = "Microsoft.Extensions.Logging.ILoggerFactory";
        private const string LoggerProviderMetadataName = "Microsoft.Extensions.Logging.ILoggerProvider";

        private static readonly string[] DefaultHandlerSuffixes = { "Handler" };

        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public ApplicationLogicShouldBeSignpostedAnalyzer()
        {

        }

        public ApplicationLogicShouldBeSignpostedAnalyzer(ISettingsReader settingsReader)
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
            var loggerType = startContext.Compilation.GetTypeByMetadataName(LoggerMetadataName);
            var loggerFactoryType = startContext.Compilation.GetTypeByMetadataName(LoggerFactoryMetadataName);
            var loggerProviderType = startContext.Compilation.GetTypeByMetadataName(LoggerProviderMetadataName);

            // We don't want to reinitialize if the ISettingsReader object was injected as that means we're in a unit test
            var settingsReader = _isSettingsReaderInjected && _settingsReader != null
                ? _settingsReader
                : new EditorConfigSettingsReader(startContext.Options);

            startContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, settingsReader, loggerType, loggerFactoryType, loggerProviderType),
                SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(
            SymbolAnalysisContext symbolContext,
            ISettingsReader settingsReader,
            params INamedTypeSymbol[] loggerTypes)
        {
            var namedType = (INamedTypeSymbol)symbolContext.Symbol;

            if (namedType.TypeKind != TypeKind.Class
                || namedType.IsRecord
                || namedType.IsAbstract
                || namedType.IsStatic
                || namedType.IsImplicitlyDeclared
                || namedType.Locations.Length == 0)
            {
                return;
            }

            var location = namedType.Locations[0];
            if (!IsApplicationLogicHandler(namedType, location.SourceTree, settingsReader))
            {
                return;
            }

            if (HasLoggerInjected(namedType, loggerTypes))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, location, namedType.Name);
            symbolContext.ReportDiagnostic(diagnostic);
        }

        private static bool IsApplicationLogicHandler(INamedTypeSymbol namedType, SyntaxTree syntaxTree, ISettingsReader settingsReader)
        {
            var suffixes = DefaultHandlerSuffixes;
            if (syntaxTree != null)
            {
                var configuredSuffixes = settingsReader.TryGetValue(syntaxTree, new SettingsKey(Id, HandlerSuffixConfigKey));
                if (!string.IsNullOrWhiteSpace(configuredSuffixes))
                {
                    suffixes = configuredSuffixes
                        .Split(',')
                        .Select(suffix => suffix.Trim())
                        .Where(suffix => suffix.Length > 0)
                        .ToArray();
                }
            }

            return suffixes.Any(suffix => namedType.Name.EndsWith(suffix, StringComparison.Ordinal));
        }

        private static bool HasLoggerInjected(
            INamedTypeSymbol namedType,
            params INamedTypeSymbol[] loggerTypes)
        {
            foreach (var constructor in namedType.InstanceConstructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    var parameterType = parameter.Type;
                    if (loggerTypes.Any(parameterType.IsOrImplementsInterface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
