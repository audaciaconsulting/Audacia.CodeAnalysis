using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;
using Roslynator.CSharp.Syntax;
//using Roslynator.CSharp;

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
                        

            //if (IsOrderByOrOrderByDescending(invocationInfo.InvocationExpression, context.SemanticModel, context.CancellationToken)
            //    && IsOrderByOrOrderByDescending(invocationInfo2.InvocationExpression, context.SemanticModel, context.CancellationToken))
            //{
            //    var location = context.Node.GetLocation();
            //    var kind = context.Node.Kind();
            //    var memberName = invocationInfo.Name;

            //    context.ReportDiagnostic(
            //        Diagnostic.Create(
            //        Rule,
            //        location,
            //        kind,
            //        memberName));
            //}
        }

        //private static bool IsOrderByOrOrderByDescending(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        //{
        //    IMethodSymbol methodSymbol = semanticModel.GetExtensionMethodInfo(invocationExpression, cancellationToken).Symbol;

        //    return methodSymbol?.IsName("OrderBy", "OrderByDescending") == true
        //        && SymbolUtility.IsLinqExtensionOfIEnumerableOfT(methodSymbol);
        //}

        //public static void AnalyzeImported(SyntaxNodeAnalysisContext context, in SimpleMemberInvocationExpressionInfo invocationInfo)
        //{
        //    ExpressionSyntax expression = invocationInfo.Expression;

        //    if (expression.Kind() != SyntaxKind.InvocationExpression)
        //        return;

        //    SimpleMemberInvocationExpressionInfo invocationInfo2 = SyntaxInfo.SimpleMemberInvocationExpressionInfo((InvocationExpressionSyntax)expression);

        //    if (!invocationInfo2.Success)
        //        return;

        //    if (!StringUtility.Equals(invocationInfo2.NameText, "OrderBy", "OrderByDescending"))
        //        return;

        //    if (IsOrderByOrOrderByDescending(invocationInfo.InvocationExpression, context.SemanticModel, context.CancellationToken)
        //        && IsOrderByOrOrderByDescending(invocationInfo2.InvocationExpression, context.SemanticModel, context.CancellationToken))
        //    {
        //        DiagnosticHelpers.ReportDiagnostic(
        //            context,
        //            DiagnosticDescriptors.CallThenByInsteadOfOrderBy,
        //            invocationInfo.Name,
        //            (invocationInfo.NameText == "OrderByDescending") ? "Descending" : null);
        //    }
        //}
    }

}