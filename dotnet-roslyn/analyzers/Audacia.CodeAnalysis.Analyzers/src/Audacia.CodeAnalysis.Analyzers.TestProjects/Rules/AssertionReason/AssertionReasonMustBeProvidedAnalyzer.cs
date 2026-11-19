using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.TestProjects.Common.AssertionFrameworks;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Audacia.CodeAnalysis.Analyzers.TestProjects.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.TestProjects.Rules.AssertionReason;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssertionReasonMustBeProvidedAnalyzer : DiagnosticAnalyzer
{
    public const string Id = DiagnosticId.AssertionReasonMustBeProvided;

    private const string Title = "Assertion has no reason provided";
    private const string MessageFormat = "Assertions must contain a reason. UT-01.1.";
    private const string Description = "Assertions must provide a reason for better clarity and maintainability.";

    private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

    private static readonly DiagnosticDescriptor Rule
        = new(Id, Title, MessageFormat, DiagnosticCategory.UnitTesting, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkUrl);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
    {
        if (!nodeAnalysisContext.IsXunitTestMethod())
        {
            return;
        }

        var methodDeclaration = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

        IAssertionFramework assertionFramework = null;

#pragma warning disable IDE0028 // Simplify collection initialization - collection expression arguments is currently in Preview and unsupported
        AnalyzeMethodInvocations(
            methodDeclaration,
            nodeAnalysisContext,
            nodeAnalysisContext.SemanticModel,
            new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default),
            ref assertionFramework);
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    [SuppressMessage("Maintainability", "AV1562:Do not declare a parameter as ref or out", Justification = "The assertion framework detected in one method must carry across the recursive walk of helper methods, including back up to the caller.")]
    private static void AnalyzeMethodInvocations(
        MethodDeclarationSyntax methodDeclaration,
        SyntaxNodeAnalysisContext nodeAnalysisContext,
        SemanticModel semanticModel,
        HashSet<IMethodSymbol> visitedMethods,
        ref IAssertionFramework assertionFramework)
    {
        SyntaxNode body = (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody;

        if (body == null)
        {
            return;
        }

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.IsValidAssertion(ref assertionFramework) && !assertionFramework.HasReasonArgument(invocation, semanticModel))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
                continue;
            }

            // Not a known assertion call — check whether it is a helper method call that contains assertions.
            var (helperMethod, helperSemanticModel) = invocation.ResolveHelperMethodDeclarationWithSemanticModel(semanticModel);
            if (helperMethod == null)
            {
                continue;
            }

            var methodSymbol = helperSemanticModel.GetDeclaredSymbol(helperMethod);
            if (methodSymbol == null || !visitedMethods.Add(methodSymbol))
            {
                continue;
            }

            AnalyzeMethodInvocations(helperMethod, nodeAnalysisContext, helperSemanticModel, visitedMethods, ref assertionFramework);
        }
    }
}
