using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ThenByDescendingAfterOrderBy
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ThenByDescendingAfterOrderByAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.ThenByDescendingAfterOrderBy;

        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private const string Title = "OrderByDescending statement follows OrderBy or OrderByDescending statement.";
        private const string MessageFormat = "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";
        private const string Description = "Use ThenOrderByDescending rather than OrderByDescending.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node.Kind() != SyntaxKind.InvocationExpression)
            {
                return;
            }

            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var orderByMethodNames = new[] { "OrderBy", "OrderByDescending" };

            //Get tokens descending from this invocation expression.
            //Don't include tokens inside arguments as a subsequent 'OrderBy' within an argument e.g. in a lambda passed into a select or where should not be replaced with a 'ThenBy'
            var tokens = invocationExpression.DescendantTokens(node => !node.IsKind(SyntaxKind.Argument))
                .Where(token => orderByMethodNames.Contains(token.ValueText))
                .ToList();

            //Check if invocation expression doesn't contain any 'OrderByDescending' tokens.
            if (!tokens.Any(t => t.ValueText == "OrderByDescending"))
            {
                return;
            }

            //Find last appearance of 'OrderByDescending' in the expression.
            var lastAppearance = tokens.Last(t => t.ValueText == "OrderByDescending");
            var lastAppearanceIndex = tokens.IndexOf(lastAppearance);

            //Find the first appearance of 'OrderBy' or 'OrderByDescending' in the expression.
            var firstOrderByToken = tokens.First(t => orderByMethodNames.Contains(t.ValueText));
            var firstOrderByTokenIndex = tokens.IndexOf(firstOrderByToken);

            if (firstOrderByTokenIndex < lastAppearanceIndex)
            {
                var location = lastAppearance.GetLocation();
                var kind = context.Node.Kind();
                var member = context.ContainingSymbol;
                var memberName = member.Name;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                    Rule,
                    location,
                    kind,
                    memberName));
            }
        }
    }
}