using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Common.AssertionFrameworks
{
    /// <summary>
    /// Identifies xUnit assertion calls and assertion scopes.
    /// Assertions are any calls on the <c>Assert</c> class except <c>Assert.Multiple</c>.
    /// The assertion scope is <c>Assert.Multiple(() => { ... })</c>.
    /// </summary>
    internal sealed class XunitAssertions : IAssertionFramework
    {
        private const string AssertClass = "Assert";
        private const string MultipleMethod = "Multiple";

        /// <inheritdoc />
        public bool IsAssertionCall(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var typeName = memberAccess.Expression.ToString();
                var methodName = memberAccess.Name.Identifier.ValueText;

                return string.Equals(typeName, AssertClass, StringComparison.Ordinal) &&
                       !string.Equals(methodName, MultipleMethod, StringComparison.Ordinal);
            }

            return false;
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
                return IsAssertMultipleCall(scopeInvocation);
            }

            return false;
        }

        private static bool IsAssertMultipleCall(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var typeName = memberAccess.Expression.ToString();
                var methodName = memberAccess.Name.Identifier.ValueText;

                return (string.Equals(typeName, $"Xunit.{AssertClass}", StringComparison.Ordinal) ||
                       string.Equals(typeName, AssertClass, StringComparison.Ordinal)) &&
                       string.Equals(methodName, MultipleMethod, StringComparison.Ordinal);
            }

            return false;
        }
    }
}
