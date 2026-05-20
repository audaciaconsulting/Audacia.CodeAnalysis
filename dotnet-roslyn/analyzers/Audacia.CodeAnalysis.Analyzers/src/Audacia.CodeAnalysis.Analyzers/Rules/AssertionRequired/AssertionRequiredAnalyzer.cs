using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AssertionRequiredAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.AssertionRequired;

        private const string Title = "Test method name '{0}' has no assertions";
        private const string MessageFormat = "Test methods must contain at least one assertion";
        private const string Description = "When a test method has no assertions, it should be updated to include at least one assertion.";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Maintainability, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var isTestMethod = nodeAnalysisContext.IsXunitTestMethod();
            if (!isTestMethod)
            {
                return;
            }
            
            var methodDeclaration = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var containsAssertion = HasAssertion(
                methodDeclaration,
                nodeAnalysisContext.SemanticModel,
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));

            if (!containsAssertion)
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        private static bool HasAssertion(
            MethodDeclarationSyntax methodDeclaration,
            SemanticModel semanticModel,
            HashSet<IMethodSymbol> visitedMethods)
        {
            SyntaxNode body = (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody;

            if(body == null)
            {
                return false;
            }

            var allInvocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            foreach(var invocation in allInvocations)
            {
                var framework = invocation.GetAssertionFramework();

                if (framework != null)
                {
                    return true;
                }

                // Not a known assertion call — check whether it is a helper method call that contains assertions.
                var helperMethod = invocation.ResolveHelperMethodDeclaration(semanticModel);
                if (helperMethod == null)
                {
                    continue;
                }

                var methodSymbol = semanticModel.GetDeclaredSymbol(helperMethod);
                if (methodSymbol == null || !visitedMethods.Add(methodSymbol))
                {
                    continue;
                }

                if (HasAssertion(helperMethod, semanticModel, visitedMethods))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
