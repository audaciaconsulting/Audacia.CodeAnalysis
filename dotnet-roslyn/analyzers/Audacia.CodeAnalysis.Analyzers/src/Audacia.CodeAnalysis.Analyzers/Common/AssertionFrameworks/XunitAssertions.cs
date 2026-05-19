using System;
using System.Linq;
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

            return HasNamedReasonParameter(invocation, semanticModel, parameterName);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the caller supplies the named reason parameter,
        /// either positionally or via a named argument. Returns <see langword="false"/> when the
        /// semantic model cannot resolve the method symbol or the parameter is not found on the
        /// resolved overload.
        /// </summary>
        private static bool HasNamedReasonParameter(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            string parameterName)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            var method = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

            if (method == null)
            {
                return false;
            }

            // Find the index of the target parameter in the resolved overload.
            var paramIndex = -1;
            for (var i = 0; i < method.Parameters.Length; i++)
            {
                if (string.Equals(method.Parameters[i].Name, parameterName, StringComparison.Ordinal))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                return false;
            }

            var arguments = invocation.ArgumentList.Arguments;

            // Check for an explicit named argument first (can appear anywhere).
            foreach (var arg in arguments)
            {
                if (arg.NameColon != null &&
                    string.Equals(arg.NameColon.Name.Identifier.ValueText, parameterName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // Fall back to positional: the caller supplied an argument at the parameter's position.
            return arguments.Count > paramIndex;
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
