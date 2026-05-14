using System;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions.AssertionFrameworks
{
    /// <summary>
    /// Identifies Shouldly assertion calls and assertion scopes.
    /// Assertions are extension methods whose names start with <c>Should</c>, excluding
    /// the exact <c>Should</c> method (used by FluentAssertions) and <c>ShouldSatisfyAllConditions</c>.
    /// The assertion scope is <c>subject.ShouldSatisfyAllConditions(...)</c>.
    /// </summary>
    internal sealed class ShouldlyAssertions : IAssertionFramework
    {
        private const string ShouldlyMethodPrefix = "Should";
        private const string FluentAssertionsShouldMethod = "Should";
        private const string ScopeMethod = "ShouldSatisfyAllConditions";

        /// <inheritdoc />
        public bool IsAssertionCall(InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.GetSimpleMethodName();

            if (methodName == null)
            {
                return false;
            }

            return methodName.StartsWith(ShouldlyMethodPrefix, StringComparison.Ordinal) &&
                   !string.Equals(methodName, FluentAssertionsShouldMethod, StringComparison.Ordinal) &&
                   !string.Equals(methodName, ScopeMethod, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public bool IsAssertionScopeExpression(SyntaxNode ancestor, InvocationExpressionSyntax invocation)
        {
            if (!(ancestor is AnonymousFunctionExpressionSyntax lambda))
            {
                return false;
            }

            if (lambda.Parent is ArgumentSyntax arg &&
                arg.Parent is ArgumentListSyntax argList &&
                argList.Parent is InvocationExpressionSyntax scopeInvocation)
            {
                return IsScopeCall(scopeInvocation);
            }

            return false;
        }

        private static bool IsScopeCall(InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.GetSimpleMethodName();
            return string.Equals(methodName, ScopeMethod, StringComparison.Ordinal);
        }
    }
}
