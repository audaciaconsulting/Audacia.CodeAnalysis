using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MustHaveJustification
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SupressionRequiresJustificationFixProvider)), Shared]
    public sealed class SupressionRequiresJustificationFixProvider : CodeFixProvider
    {
        /// <summary>
        /// The title of the provided fix.
        /// </summary>
        private const string Title = "Add the 'Justification' arguement to the attribute";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(SupressionRequiresJustificationAnalyzer.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);

                if (node is AttributeSyntax attribute)
                {
                    // In this case there is no justification at all
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            Title,
                            token => AddJustificationToAttributeAsync(context.Document, root, attribute),
                            SupressionRequiresJustificationAnalyzer.Id),
                        diagnostic);
                    return;
                }
                else if (node is AttributeArgumentSyntax argument)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            Title,
                            token => UpdateValueOfArgumentAsync(context.Document, root, argument),
                            SupressionRequiresJustificationAnalyzer.Id),
                        diagnostic);
                    return;
                }
            }
        }

        private static Task<Document> UpdateValueOfArgumentAsync(Document document, SyntaxNode root, AttributeArgumentSyntax argument)
        {
            var newArgument = argument.WithExpression(GetNewAttributeValue());
            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(argument, newArgument)));
        }

        private static Task<Document> AddJustificationToAttributeAsync(Document document, SyntaxNode syntaxRoot, AttributeSyntax attribute)
        {
            var arguementName = SyntaxFactory.IdentifierName(nameof(SuppressMessageAttribute.Justification));
            var newArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(arguementName), null, GetNewAttributeValue());

            var newArgumentList = attribute.ArgumentList.AddArguments(newArgument);
            return Task.FromResult(document.WithSyntaxRoot(syntaxRoot.ReplaceNode(attribute.ArgumentList, newArgumentList)));
        }

        private static LiteralExpressionSyntax GetNewAttributeValue()
        {
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(SupressionRequiresJustificationAnalyzer.JustificationPlaceholder));
        }
    }
}
