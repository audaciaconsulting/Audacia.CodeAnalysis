using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Rules.SuppressionRequiresJustification
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuppressionRequiresJustificationFixProvider)), Shared]
    public sealed class SuppressionRequiresJustificationFixProvider : CodeFixProvider
    {
        /// <summary>
        /// The title of the provided fix.
        /// </summary>
        private const string Title = "Add the 'Justification' argument to the attribute";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(SuppressionRequiresJustificationAnalyzer.Id);

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
                    RegisterCodeFix(context, root, diagnostic, attribute);
                    return;
                }
            }
        }

        private static void RegisterCodeFix(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic, AttributeSyntax attribute)
        {
            var justificationArgument = attribute.ArgumentList.Arguments
                .FirstOrDefault(argument =>
                    argument.NameEquals?.Name.Identifier.ValueText == SuppressionRequiresJustificationAnalyzer.JustificationName);

            if (justificationArgument == null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        Title,
                        token => AddJustificationToAttributeAsync(context.Document, root, attribute),
                        SuppressionRequiresJustificationAnalyzer.Id),
                    diagnostic);
            }
            else
            {
                var valueEqualsPlaceholder = justificationArgument?.Expression is LiteralExpressionSyntax literal &&
                    literal.Token.ValueText == SuppressionRequiresJustificationAnalyzer.JustificationPlaceholder;

                if (!valueEqualsPlaceholder)
                {
                    context.RegisterCodeFix(
                    CodeAction.Create(
                        Title,
                        token => UpdateValueOfArgumentAsync(context.Document, root, justificationArgument),
                        SuppressionRequiresJustificationAnalyzer.Id),
                    diagnostic);
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
            var argumentName = SyntaxFactory.IdentifierName(nameof(SuppressMessageAttribute.Justification));
            var newArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(argumentName), null, GetNewAttributeValue());
            var newArgumentList = attribute.ArgumentList.AddArguments(newArgument);
            return Task.FromResult(document.WithSyntaxRoot(syntaxRoot.ReplaceNode(attribute.ArgumentList, newArgumentList)));
        }

        private static LiteralExpressionSyntax GetNewAttributeValue()
        {
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(SuppressionRequiresJustificationAnalyzer.JustificationPlaceholder));
        }
    }
}
