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
            var assertionCount = CountAssertionsOutsideAssertionScopes(methodDeclaration);

            if (assertionCount > MaxAssertionsOutsideScope)
            {
                var methodName = methodDeclaration.Identifier.ValueText;
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Counts all assertion calls in the method body that are not enclosed by the matching framework's assertion scope.
        /// </summary>
        private static int CountAssertionsOutsideAssertionScopes(MethodDeclarationSyntax method)
        {
            SyntaxNode body = (SyntaxNode)method.Body ?? method.ExpressionBody;
            var allInvocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            var count = 0;
            foreach (var invocation in allInvocations)
            {
                var framework = GetAssertionFramework(invocation);

                if (framework == null)
                {
                    continue;
                }

                var isInsideScope = invocation.Ancestors().Any(ancestor => framework.IsAssertionScopeExpression(ancestor, invocation));

                if (!isInsideScope)
                {
                    count++;
                }
            }

            return count;
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