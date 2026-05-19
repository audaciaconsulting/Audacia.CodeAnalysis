using System;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Common.AssertionFrameworks
{
    /// <summary>
    /// Identifies FluentAssertions assertion calls and assertion scopes.
    /// Assertions are terminal calls in an invocation chain that contains a <c>.Should()</c> call,
    /// e.g. the <c>.Be(42)</c> in <c>result.Should().Be(42)</c>.
    /// The assertion scope is <c>using (new AssertionScope()) { ... }</c> or
    /// <c>using var _ = new AssertionScope();</c>.
    /// </summary>
    internal sealed class FluentAssertions : IAssertionFramework
    {
        private const string ShouldMethod = "Should";
        private const string AssertionScopeType = "AssertionScope";

        /// <inheritdoc />
        public bool IsAssertionCall(InvocationExpressionSyntax invocation)
        {
            // The .Should() call itself is not the assertion; it is the start of the chain.
            var methodName = invocation.GetSimpleMethodName();
            if (string.Equals(methodName, ShouldMethod, StringComparison.Ordinal))
            {
                return false;
            }

            // Walk down the member-access / invocation chain looking for a .Should() call.
            var current = (ExpressionSyntax)invocation;
            while (current != null)
            {
                if (current is InvocationExpressionSyntax inv)
                {
                    var name = inv.GetSimpleMethodName();
                    if (string.Equals(name, ShouldMethod, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    current = inv.Expression is MemberAccessExpressionSyntax ma ? ma.Expression : null;
                }
                else if (current is MemberAccessExpressionSyntax memberAccess)
                {
                    current = memberAccess.Expression;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// FluentAssertions uses a parameter named <c>because</c> on every assertion method,
        /// e.g. <c>result.Should().Be(42, because: "reason")</c>.
        /// If no <c>because</c> parameter exists on the resolved overload the method returns
        /// <see langword="true"/> so no spurious diagnostic is raised.
        /// </remarks>
        public bool HasReasonArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            return HasNamedReasonParameter(invocation, semanticModel, "because");
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

        /// <inheritdoc />
        public bool IsAssertionScopeExpression(SyntaxNode ancestor, InvocationExpressionSyntax invocation)
        {
            if (ancestor is UsingStatementSyntax usingStatement && IsAssertionScopeUsing(usingStatement))
            {
                return true;
            }

            if (ancestor is BlockSyntax block)
            {
                var hasDeclarationScope = block.Statements
                    .TakeWhile(s => !s.Contains(invocation))
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Any(IsAssertionScopeDeclaration);

                if (hasDeclarationScope)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAssertionScopeUsing(UsingStatementSyntax usingStatement)
        {
            // using (new AssertionScope()) — expression form
            if (usingStatement.Expression is ObjectCreationExpressionSyntax creation)
            {
                return IsAssertionScopeCreation(creation);
            }

            // using (AssertionScope scope = new AssertionScope()) — declaration form
            if (usingStatement.Declaration != null)
            {
                return usingStatement.Declaration.Variables
                    .Any(v => v.Initializer?.Value is ObjectCreationExpressionSyntax varCreation &&
                              IsAssertionScopeCreation(varCreation));
            }

            return false;
        }

        private static bool IsAssertionScopeDeclaration(LocalDeclarationStatementSyntax declaration)
        {
            return declaration.Declaration.Variables
                .Any(v => v.Initializer?.Value is ObjectCreationExpressionSyntax creation &&
                          IsAssertionScopeCreation(creation));
        }

        private static bool IsAssertionScopeCreation(ObjectCreationExpressionSyntax creation)
        {
            var typeName = creation.Type.ToString();
            return typeName == AssertionScopeType ||
                   typeName.EndsWith("." + AssertionScopeType, StringComparison.Ordinal);
        }
    }
}
