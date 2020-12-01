using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ThenOrderByDescendingAfterOrderBy
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ThenOrderByDescendingAfterOrderByAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.ThenOrderByDescendingAfterOrderBy;

        private const string Title = "OrderByDescending statement follows OrderBy or OrderByDescending statement.";
        private const string MessageFormat = "ThenOrderByDescending statement should follow OrderBy or OrderByDescending statement.";
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
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(invocationExpression);

            //Check if invocation expression contains OrderByDescending string.
            if (!invocationExpression.GetText().ToString().Contains("OrderByDescending"))
            {
                return;
            }

            //Get tokens descending from this invocation expression.
            var tokens = invocationExpression.DescendantTokens()
                .Where(token => token.ValueText == "OrderBy" || token.ValueText == "OrderByDescending")
                .ToList();

            //Find last appearance of 'OrderByDescending' in the expression.
            var lastAppearance = tokens.Last(t => t.ValueText == "OrderByDescending");
            var lastAppearanceIndex = tokens.IndexOf(lastAppearance);

            //Find the first appearance of 'OrderBy' or 'OrderByDescending' in the expression.
            var firstOrderByToken = tokens.First(t => t.ValueText == "OrderBy" || t.ValueText == "OrderByDescending");
            var firstOrderByTokenIndex = tokens.IndexOf(firstOrderByToken);


            if (firstOrderByTokenIndex < lastAppearanceIndex)
            {
                var location = context.Node.GetLocation();
                var kind = context.Node.Kind();
                ISymbol member = context.ContainingSymbol;
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