using System;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Common.AssertionFrameworks
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
        private const string ReasonParameterName = "customMessage";

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

        /// <inheritdoc />
        /// <remarks>
        /// Shouldly uses a parameter named <c>customMessage</c> on overloads that accept a message,
        /// e.g. <c>foo.ShouldBe(42, "reason")</c> maps to <c>customMessage</c>.
        /// If no <c>customMessage</c> parameter exists on the resolved overload the method returns
        /// <see langword="true"/> so no spurious diagnostic is raised.
        /// </remarks>
        public bool HasReasonArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var paramIndex = invocation.FindParameterIndex(ReasonParameterName, semanticModel);

            // For Shouldly, if there is no parameter named "customMessage", then the assertion does not support a reason (the methods are not overloaded, but use default parameter values).
            // So we should not report a diagnostic.
            if (paramIndex == -1)
            {
                return true;
            }

            return invocation.HasParameter(ReasonParameterName, paramIndex);
        }

        private static bool IsScopeCall(InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.GetSimpleMethodName();
            return string.Equals(methodName, ScopeMethod, StringComparison.Ordinal);
        }
    }
}
