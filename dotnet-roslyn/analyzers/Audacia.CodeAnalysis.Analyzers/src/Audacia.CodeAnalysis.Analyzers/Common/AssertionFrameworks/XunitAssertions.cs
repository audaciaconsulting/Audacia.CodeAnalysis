using System;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Extensions;
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

        /// <summary>
        /// Maps the names of xUnit <c>Assert</c> methods that support a user-facing failure message
        /// to the name of the parameter that carries that message.
        /// Methods absent from this map do not accept a failure message in any overload and are
        /// therefore excluded from the reason check.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> MessageParameterByMethod =
            new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "True",  "userMessage" },
                { "False", "userMessage" },
                { "Fail",  "message" },
            };

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

        /// <inheritdoc/>
        /// <remarks>
        /// Only the xUnit methods listed in <see cref="MessageParameterByMethod"/> support a
        /// user-facing failure message. All other <c>Assert.*</c> methods have no such parameter
        /// in any overload, so they are excluded from the reason check and always return
        /// <see langword="true"/>.
        /// </remarks>
        public bool HasReasonArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return true;
            }

            var methodName = memberAccess.Name.Identifier.ValueText;

            if (!MessageParameterByMethod.TryGetValue(methodName, out var parameterName))
            {
                // This method has no message parameter in any overload — nothing to enforce.
                return true;
            }

            var paramIndex = invocation.FindParameterIndex(parameterName, semanticModel);

            // The invocation is using an overload without the reason parameter, so report that it is missing.
            if (paramIndex == -1)
            {
                return false;
            }

            return invocation.HasExplicitlyNamedParameter(parameterName, paramIndex);
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
