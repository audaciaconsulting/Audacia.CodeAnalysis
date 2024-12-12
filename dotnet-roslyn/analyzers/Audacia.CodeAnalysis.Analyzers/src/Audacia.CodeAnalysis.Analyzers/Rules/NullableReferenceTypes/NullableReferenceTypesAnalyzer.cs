using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.NullableReferenceTypes
{
    /// <summary>
    /// Analyzer that checks if nullable reference types are enabled at the project level.
    /// If not, a warning is issued as a project-level diagnostic.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullableReferenceTypesAnalyzer : DiagnosticAnalyzer
    {
        private const string Id = DiagnosticId.NullableReferenceTypesEnabled;

        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        /// <summary>
        /// Add the 'CompilationEnd' tag because we are reporting a compilation-level diagnostic.
        /// </summary>
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            DiagnosticMessages.NullableReferenceTypes.Title,
            DiagnosticMessages.NullableReferenceTypes.MessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: DiagnosticMessages.NullableReferenceTypes.Description,
            helpLinkUri: HelpLinkUrl,
            customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Directly register a compilation action.
            // This action runs once per compilation and is suitable for reporting a project-level diagnostic.
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var nullableContextOptions = context.Compilation.Options.NullableContextOptions;

            if (nullableContextOptions == NullableContextOptions.Disable)
            {
                // Report a diagnostic with no location for a project-level warning.
                var diagnostic = Diagnostic.Create(Rule, Location.None);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
