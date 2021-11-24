using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;

namespace Audacia.CodeAnalysis.Analyzers.Rules.FieldWithUnderscore
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldWithUnderscoreCodeFixProvider)), Shared]
    public sealed class FieldWithUnderscoreCodeFixProvider : CodeFixProvider
    {
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(FieldWithUnderscoreAnalyzer.Id);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            context.RegisterCodeFix(
                CodeAction.Create("Prepend '_' to field", c => PrependUnderscore(context.Document, token, c), FieldWithUnderscoreAnalyzer.Id),
                diagnostic);
        }

        private async Task<Solution> PrependUnderscore(Document document, SyntaxToken declaration, CancellationToken cancellationToken)
        {
            var newName = $"_{declaration.ValueText}";
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(declaration.Parent, cancellationToken);
            var solution = document.Project.Solution;

            return await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
        }
	}
}
