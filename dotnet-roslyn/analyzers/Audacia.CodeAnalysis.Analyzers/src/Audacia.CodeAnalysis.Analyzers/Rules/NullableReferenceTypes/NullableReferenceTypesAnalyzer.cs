using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.NullableReferenceTypes
{
    /// <summary>
    /// Analyzer that is triggered if nullable reference types are not enabled on a project.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullableReferenceTypesAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic rule for the nullable reference type analyzer. 
        /// </summary>
        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(
                DiagnosticId.NullableReferenceTypesEnabled,
                DiagnosticMessages.NullableReferenceTypes.Title,
                DiagnosticMessages.NullableReferenceTypes.MessageFormat,
                DiagnosticCategory.Maintainability,
                DiagnosticSeverity.Warning,
                true,
                DiagnosticMessages.NullableReferenceTypes.Description);

        /// <summary>
        /// The collection of diagnostics for this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        /// <summary>
        /// Initializes the logic for this analyzer.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Since we're analyzing the .csproj file, this must be set to 'analyze'.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(analysisContext => 
                analysisContext.RegisterSemanticModelAction(AnalyzeNullableContextOptions));
        }

        /// <summary>
        /// Evaluates whether nullable options are enabled on the given <paramref name="context"/>.
        /// If not, the diagnostic rule is triggered.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        private static void AnalyzeNullableContextOptions(SemanticModelAnalysisContext context)
        {
            var nullableContextOptions = context.SemanticModel
                .Compilation
                .Options
                .NullableContextOptions;

            // Allow all possible options, excluding 'Disable' (which is the assumed value if the node is missing altogether).
            if (nullableContextOptions != NullableContextOptions.Disable)
            {
                return;
            }

            var location = context.SemanticModel
                .SyntaxTree
                .GetRoot()
                .GetLocation();

            var diagnostic = Diagnostic.Create(Rule, location);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
