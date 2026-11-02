using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.UseInitForRecordSetters
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseInitForRecordSettersAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.UseInitForRecordSetters;
        public static DiagnosticSeverity Severity => DiagnosticSeverity.Warning;

        private const string Title = "Record property setter should be init-only";
        private const string MessageFormat = "Record property '{0}' in record type '{1}' should be init-only";
        private const string Description = "Record properties should use 'init' instead of 'set', to ensure immutability.";
        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Usage,
            Severity,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl); 
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            startContext.RegisterSymbolAction(actionContext => AnalyzeSymbol(actionContext), SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (!type.IsRecord)
            {
                return;
            }

            var properties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsReadOnly && p.SetMethod != null && !p.SetMethod.IsInitOnly)
                .Where(p => !p.IsStatic && !p.IsOverride);

            foreach (var property in properties)
            {
                ReportDiagnostic(context, type, property);
            }
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, INamedTypeSymbol type, IPropertySymbol property)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                property.SetMethod.Locations.FirstOrDefault() ?? property.Locations[0],
                property.Name,
                type.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
