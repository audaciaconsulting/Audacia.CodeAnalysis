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
using Microsoft.CodeAnalysis.Rename;

namespace Audacia.CodeAnalysis.Analyzers.Rules.UseRecordTypes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseRecordTypesCodeFixProvider)), Shared]
    public class UseRecordTypesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseRecordTypesAnalyzer.Id);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var node = (ClassDeclarationSyntax)root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
            var codeAction = CodeAction.Create("Change to record type",
                c => ChangeToRecord(context.Document, node, root, c),
                UseRecordTypesAnalyzer.Id);
            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private Task<Document> ChangeToRecord(Document document, ClassDeclarationSyntax classDeclaration,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var newKeyword = SyntaxFactory.Token(SyntaxKind.RecordKeyword);

            var recordDeclaration = SyntaxFactory.RecordDeclaration(
                classDeclaration.AttributeLists, classDeclaration.Modifiers, newKeyword, classDeclaration.Identifier,
                classDeclaration.TypeParameterList, classDeclaration.ParameterList, classDeclaration.BaseList,
                classDeclaration.ConstraintClauses, classDeclaration.OpenBraceToken, classDeclaration.Members,
                classDeclaration.CloseBraceToken, classDeclaration.SemicolonToken);

            var newRoot = root.ReplaceNode(classDeclaration, recordDeclaration);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}