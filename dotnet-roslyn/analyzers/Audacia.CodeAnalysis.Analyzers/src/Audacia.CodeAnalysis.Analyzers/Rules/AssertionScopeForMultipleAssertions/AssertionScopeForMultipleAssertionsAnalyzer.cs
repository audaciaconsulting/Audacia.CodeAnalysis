using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions.AssertionFrameworks;
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

        private static readonly IAssertionFramework[] AssertionFrameworks =
        {
            new XunitAssertions(),
            new FluentAssertions(),
            new ShouldlyAssertions()
        };

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

            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
            {
                return;
            }

            // Count the number of assertions outside of an assertion scope. If there are more than two, report a diagnostic.
            var assertionCount = CountAssertionsOutsideAssertionScopes(
                methodDeclaration,
                nodeAnalysisContext.SemanticModel,
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));

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
            HashSet<IMethodSymbol> visitedMethods)
        {
            SyntaxNode body = (SyntaxNode)method.Body ?? method.ExpressionBody;
            var allInvocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var count = 0;
            foreach (var invocation in allInvocations)
            {
                var framework = GetAssertionFramework(invocation);

                if (framework != null)
                {
                    var isInsideScope = invocation.Ancestors().Any(ancestor => framework.IsAssertionScopeExpression(ancestor, invocation));

                    if (!isInsideScope)
                    {
                        count++;
                    }

                    continue;
                }

                // Not a known assertion call — check whether it is a helper method call that contains assertions.
                // If the call site itself is already inside a scope, we treat all assertions inside the helper as scoped.
                var isCallSiteInsideScope = IsInvocationInsideAnyAssertionScope(invocation);
                if (isCallSiteInsideScope)
                {
                    continue;
                }

                var helperMethod = ResolveHelperMethodDeclaration(invocation, semanticModel);
                if (helperMethod == null)
                {
                    continue;
                }

                var methodSymbol = semanticModel.GetDeclaredSymbol(helperMethod);
                if (methodSymbol == null || !visitedMethods.Add(methodSymbol))
                {
                    continue;
                }

                count += CountAssertionsOutsideAssertionScopes(helperMethod, semanticModel, visitedMethods);
            }

            return count;
        }

        /// <summary>
        /// Returns true if the invocation is nested inside any known assertion scope expression.
        /// </summary>
        private static bool IsInvocationInsideAnyAssertionScope(InvocationExpressionSyntax invocation)
        {
            return invocation.Ancestors().Any(ancestor =>
                AssertionFrameworks.Any(framework => framework.IsAssertionScopeExpression(ancestor, invocation)));
        }

        /// <summary>
        /// Uses the semantic model to resolve <paramref name="invocation"/> to the <see cref="MethodDeclarationSyntax"/>
        /// declared in the same compilation, or returns <see langword="null"/> if not found.
        /// </summary>
        private static MethodDeclarationSyntax ResolveHelperMethodDeclaration(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            var symbol = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

            if (symbol == null)
            {
                return null;
            }

            var declaration = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
            return declaration;
        }

        /// <summary>
        /// Returns the <see cref="IAssertionFramework"/> that recognises <paramref name="invocation"/> as one
        /// of its assertion calls, or <see langword="null"/> if the invocation is not a known assertion.
        /// </summary>
        private static IAssertionFramework GetAssertionFramework(InvocationExpressionSyntax invocation)
        {
            return AssertionFrameworks.FirstOrDefault(framework => framework.IsAssertionCall(invocation));
        }
    }
}