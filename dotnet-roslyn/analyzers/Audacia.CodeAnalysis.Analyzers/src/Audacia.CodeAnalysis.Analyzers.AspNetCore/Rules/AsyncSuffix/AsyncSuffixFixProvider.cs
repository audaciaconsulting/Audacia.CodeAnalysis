using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Audacia.CodeAnalysis.Analyzers.AspNetCore.Rules.AsyncSuffix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncSuffixFixProvider)), Shared]
    public sealed class AsyncSuffixFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(AsyncSuffixAnalyzer.Id);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();

            // For this analyser, the source span encapsulates the entire method, so we need to find the method name token within it
            var methodNode = root.FindNode(diagnostic.Location.SourceSpan);
            var token = ((MethodDeclarationSyntax)methodNode).Identifier;

            context.RegisterCodeFix(
                CodeAction.Create("Add 'Async' suffix to method name", c => AppendAsync(context.Document, token, c),
                    AsyncSuffixAnalyzer.Id),
                diagnostic);
        }

        private async Task<Solution> AppendAsync(Document document, SyntaxToken declaration,
            CancellationToken cancellationToken)
        {
            var newName = $"{declaration.ValueText}Async";
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(declaration.Parent, cancellationToken);
            var solution = document.Project.Solution;

            return await Renamer
                .RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
