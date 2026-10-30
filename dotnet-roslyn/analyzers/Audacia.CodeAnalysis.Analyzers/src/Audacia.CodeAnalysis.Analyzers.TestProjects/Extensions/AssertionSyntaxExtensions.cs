using System.Linq;
using Audacia.CodeAnalysis.Analyzers.TestProjects.Common.AssertionFrameworks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.TestProjects.Extensions
{
    /// <summary>
    /// Assertion-related extensions to <see cref="InvocationExpressionSyntax"/>.
    /// </summary>
    public static class AssertionSyntaxExtensions
    {
        private static readonly IAssertionFramework[] AssertionFrameworks =
        {
            new XunitAssertions(),
            new FluentAssertions(),
            new ShouldlyAssertions()
        };

        /// <summary>
        /// Returns the <see cref="IAssertionFramework"/> that recognises <paramref name="invocation"/> as one
        /// of its assertion calls, or <see langword="null"/> if the invocation is not a known assertion.
        /// </summary>
        public static IAssertionFramework GetAssertionFramework(this InvocationExpressionSyntax invocation)
        {
            return AssertionFrameworks.FirstOrDefault(framework => framework.IsAssertionCall(invocation));
        }

        /// <summary>
        /// Returns the <see cref="IAssertionFramework"/> that recognises <paramref name="invocation"/> as being
        /// nested inside one of its assertion scopes, or <see langword="null"/> if the invocation is not nested inside any known assertion scope.
        /// </summary>
        public static IAssertionFramework GetAssertionScopeFramework(this InvocationExpressionSyntax invocation)
        {
            return AssertionFrameworks.FirstOrDefault(framework =>
                invocation.Ancestors().Any(ancestor => framework.IsAssertionScopeExpression(ancestor, invocation)));
        }

        /// <summary>
        /// Determines whether the <paramref name="invocation"/> is a valid test assertion call, using the <paramref name="assertionFramework"/> if set.
        /// If it is found to be a valid assertion then <paramref name="assertionFramework"/> is set to the framework which it is valid for, if it wasn't
        /// already set.
        /// </summary>
        public static bool IsValidAssertion(this InvocationExpressionSyntax invocation, ref IAssertionFramework assertionFramework)
        {
            var validAssertion = false;
            if (assertionFramework == null)
            {
                assertionFramework = invocation.GetAssertionFramework();
                validAssertion = assertionFramework != null;
            }

            return validAssertion || assertionFramework?.IsAssertionCall(invocation) == true;
        }

        /// <summary>
        /// Determines whether the given <paramref name="invocation"/> is nested inside an assertion scope of the given <paramref name="framework"/>.
        /// </summary>
        public static bool IsInsideAssertionScope(this InvocationExpressionSyntax invocation, IAssertionFramework framework)
        {
            return invocation.Ancestors().Any(ancestor => framework.IsAssertionScopeExpression(ancestor, invocation));
        }
    }
}
