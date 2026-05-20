using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Common.AssertionFrameworks;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AssertionScopeForMultipleAssertionsAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.AssertionScopeForMultipleAssertions;

        private const string Title = "Test method name '{0}' has multiple assertions outside an assertion scope";
        private const string MessageFormat = "Use an assertion scope when a test method has more than two assertions";
        private const string Description = "When a test method has more than two assertions, they should be wrapped within an assertion scope to provide a stack of all failures.";

        private const int MaxAssertionsOutsideScope = 2;

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

            // Count the number of assertions outside of an assertion scope. If there are more than two, report a diagnostic.
            IAssertionFramework assertionFramework = null;
            var assertionCount = CountAssertionsOutsideAssertionScopes(
                methodDeclaration,
                nodeAnalysisContext.SemanticModel,
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default),
                ref assertionFramework);

            if (assertionCount > MaxAssertionsOutsideScope)
            {
                var methodName = methodDeclaration.Identifier.ValueText;
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Counts all assertion calls in the method body that are not enclosed by the matching framework's assertion scope.
        /// Helper method calls are followed recursively, and assertions inside them that have their own scope are not counted.
        /// </summary>
        private static int CountAssertionsOutsideAssertionScopes(
            MethodDeclarationSyntax method,
            SemanticModel semanticModel,
            HashSet<IMethodSymbol> visitedMethods,
            ref IAssertionFramework assertionFramework)
        {
            SyntaxNode body = (SyntaxNode)method.Body ?? method.ExpressionBody;

            if (body == null)
            {
                return 0;
            }

            var allInvocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var count = 0;
            foreach (var invocation in allInvocations)
            {
                var validAssertion = false;
                if (assertionFramework == null)
                {
                    assertionFramework = invocation.GetAssertionFramework();
                    validAssertion = assertionFramework != null;
                }

                validAssertion = validAssertion || assertionFramework?.IsAssertionCall(invocation) == true;

                if (validAssertion)
                {
                    var isInsideScope = invocation.IsInsideAssertionScope(assertionFramework);

                    if (!isInsideScope)
                    {
                        count++;
                    }

                    continue;
                }

                // Not a known assertion call — check whether it is a helper method call that contains assertions.
                // If the call site itself is already inside a scope, we treat all assertions inside the helper as scoped.
                var isCallSiteInsideScope = invocation.GetAssertionScopeFramework() != null;
                if (isCallSiteInsideScope)
                {
                    continue;
                }

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

                count += CountAssertionsOutsideAssertionScopes(helperMethod, semanticModel, visitedMethods, ref assertionFramework);
            }

            return count;
        }
    }
}