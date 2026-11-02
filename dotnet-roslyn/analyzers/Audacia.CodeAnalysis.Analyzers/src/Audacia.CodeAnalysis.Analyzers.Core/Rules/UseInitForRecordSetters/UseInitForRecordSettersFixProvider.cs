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
using Microsoft.CodeAnalysis.FindSymbols;

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

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (!(semanticModel.GetDeclaredSymbol(setter, context.CancellationToken)?.AssociatedSymbol is IPropertySymbol property)
                || await ImplementsInterfaceSetterInDerivedRecordAsync(property, context.Document.Project.Solution, context.CancellationToken).ConfigureAwait(false))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create("Replace 'set' with 'init'", c => ReplaceSetWithInitAsync(context.Document, setter, c), UseInitForRecordSettersAnalyzer.Id),
                diagnostic);
        }

        /// <summary>
        /// Checks for derived records anywhere in the solution that implement an interface property with a 'set' accessor through this property (or an override of it).
        /// </summary>
        private static async Task<bool> ImplementsInterfaceSetterInDerivedRecordAsync(IPropertySymbol property, Solution solution, CancellationToken cancellationToken)
        {
            var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(property.ContainingType, solution, transitive: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Compared by source location, as symbols from another project's compilation are not equal to this one's.
            return derivedTypes
                .SelectMany(UseInitForRecordSettersAnalyzer.GetInterfaceSetterImplementations)
                .Any(implementation => implementation.Locations.Any(property.Locations.Contains));
        }

        /// <summary>
        /// Replaces 'set' with 'init' on the property and all of its overrides, as an override must match the init-only status of the property it overrides (CS8853).
        /// </summary>
        private static async Task<Solution> ReplaceSetWithInitAsync(Document document, AccessorDeclarationSyntax setter, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var property = (IPropertySymbol)semanticModel.GetDeclaredSymbol(setter, cancellationToken).AssociatedSymbol;
            var overrides = await SymbolFinder.FindOverridesAsync(property, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            var setterReferences = new[] { property }
                .Concat(overrides.OfType<IPropertySymbol>())
                .Where(p => p.SetMethod != null && !p.SetMethod.IsInitOnly)
                .SelectMany(p => p.SetMethod.DeclaringSyntaxReferences)
                .GroupBy(reference => (reference.SyntaxTree, reference.Span))
                .Select(group => group.First());

            var editor = new SolutionEditor(solution);

            foreach (var reference in setterReferences)
            {
                var setterDocument = solution.GetDocument(reference.SyntaxTree);
                if (setterDocument == null || !(await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is AccessorDeclarationSyntax setterToReplace))
                {
                    continue;
                }

                var documentEditor = await editor.GetDocumentEditorAsync(setterDocument.Id, cancellationToken).ConfigureAwait(false);
                documentEditor.ReplaceNode(setterToReplace, ToInitAccessor(setterToReplace));
            }

            return editor.GetChangedSolution();
        }

        private static AccessorDeclarationSyntax ToInitAccessor(AccessorDeclarationSyntax setter)
        {
            return SyntaxFactory.AccessorDeclaration(
                SyntaxKind.InitAccessorDeclaration,
                setter.AttributeLists,
                setter.Modifiers,
                SyntaxFactory.Token(SyntaxKind.InitKeyword).WithTriviaFrom(setter.Keyword),
                setter.Body,
                setter.ExpressionBody,
                setter.SemicolonToken);
        }
    }
}
