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
            return HasNamedReasonParameter(invocation, semanticModel, "customMessage");
        }

        /// <summary>
        /// Returns <see langword="true"/> when the resolved method symbol for <paramref name="invocation"/>
        /// declares a parameter with <paramref name="parameterName"/> and the caller either passes a
        /// positional argument at that parameter's index or uses a named argument with that name.
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

            // This overload does not have the reason parameter at all — nothing to check.
            if (paramIndex == -1)
            {
                return true;
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

        private static bool IsScopeCall(InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.GetSimpleMethodName();
            return string.Equals(methodName, ScopeMethod, StringComparison.Ordinal);
        }
    }
}
