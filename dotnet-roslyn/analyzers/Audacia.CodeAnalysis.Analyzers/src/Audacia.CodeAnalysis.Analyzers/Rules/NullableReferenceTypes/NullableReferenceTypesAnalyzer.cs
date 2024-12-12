using System.Collections.Immutable;
using System.Linq;
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
        private const string Id = DiagnosticId.NullableReferenceTypesEnabled;
        
        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);
        
        /// <summary>
        /// The diagnostic rule for the nullable reference type analyzer. 
        /// </summary>
        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(
                Id,
                DiagnosticMessages.NullableReferenceTypes.Title,
                DiagnosticMessages.NullableReferenceTypes.MessageFormat,
                DiagnosticCategory.Maintainability,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                DiagnosticMessages.NullableReferenceTypes.Description,
                HelpLinkUrl);

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

        private static void AnalyzeNullableContextOptions(SemanticModelAnalysisContext context)
        {
            var compilation = context.SemanticModel.Compilation;
            var nullableContextOptions = compilation.Options.NullableContextOptions;

            // If nullable reference types are enabled, no diagnostic is reported.
            if (nullableContextOptions != NullableContextOptions.Disable)
            {
                return;
            }

            // Identify the class declaration node of this analyzer class.
            var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);
            var classDeclaration = root
                .DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .FirstOrDefault(cd => cd.Identifier.ValueText == nameof(NullableReferenceTypesAnalyzer));

            // If we found the class declaration, use its location; otherwise, fallback to the file root.
            var location = classDeclaration?.GetLocation() ?? root.GetLocation();

            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }

    }
}
