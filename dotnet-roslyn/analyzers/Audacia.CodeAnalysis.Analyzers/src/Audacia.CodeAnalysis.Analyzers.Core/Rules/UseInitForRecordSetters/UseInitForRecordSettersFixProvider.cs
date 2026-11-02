using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Audacia.CodeAnalysis.Analyzers.Core.Rules.UseInitForRecordSetters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseInitForRecordSettersFixProvider)), Shared]
    public sealed class UseInitForRecordSettersFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseInitForRecordSettersAnalyzer.Id);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);

            if (!(token.Parent is AccessorDeclarationSyntax setter))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create("Replace 'set' with 'init'", c => ReplaceSetWithInitAsync(context.Document, setter, c), UseInitForRecordSettersAnalyzer.Id),
                diagnostic);
        }

        private static async Task<Document> ReplaceSetWithInitAsync(Document document, AccessorDeclarationSyntax setter, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(setter, SyntaxFactory.AccessorDeclaration(
                SyntaxKind.InitAccessorDeclaration,
                setter.AttributeLists,
                setter.Modifiers,
                SyntaxFactory.Token(SyntaxKind.InitKeyword).WithTriviaFrom(setter.Keyword),
                setter.Body,
                setter.ExpressionBody,
                setter.SemicolonToken));

            return editor.GetChangedDocument();
        }
    }
}
