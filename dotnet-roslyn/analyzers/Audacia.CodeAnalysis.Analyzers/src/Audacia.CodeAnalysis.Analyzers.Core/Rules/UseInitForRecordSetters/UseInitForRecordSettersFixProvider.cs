using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audacia.CodeAnalysis.Analyzers.Rules.FieldWithUnderscore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Audacia.CodeAnalysis.Analyzers.Rules.UseInitForRecordSetters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldWithUnderscoreCodeFixProvider)), Shared]
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

            context.RegisterCodeFix(
                CodeAction.Create("Replace 'set' with 'init'", c => ReplaceSetWithInit(context.Document, token, c), UseInitForRecordSettersAnalyzer.Id),
                diagnostic);
        }

        private async Task<Document> ReplaceSetWithInit(Document document, SyntaxToken token, CancellationToken c)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken: c);
            var setter = token.Parent as AccessorDeclarationSyntax;

            if (setter == null)
            {
                return document;
            }

            editor.ReplaceNode(setter, SyntaxFactory.AccessorDeclaration(
                SyntaxKind.InitAccessorDeclaration,
                setter.AttributeLists,
                setter.Modifiers,
                SyntaxFactory.Token(SyntaxKind.InitKeyword).WithTriviaFrom(token),
                setter.Body,
                setter.ExpressionBody,
                setter.SemicolonToken));
            
            var newDocument = editor.GetChangedDocument();
            return newDocument;
        }
    }
}
