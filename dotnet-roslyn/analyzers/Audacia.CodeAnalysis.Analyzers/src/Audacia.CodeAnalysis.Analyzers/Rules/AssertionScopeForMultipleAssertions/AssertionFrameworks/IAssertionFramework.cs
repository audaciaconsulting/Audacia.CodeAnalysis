using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions.AssertionFrameworks
{
    /// <summary>
    /// Represents a test assertion framework and provides the ability to identify assertion calls
    /// and assertion scope expressions within that framework's syntax.
    /// </summary>
    internal interface IAssertionFramework
    {
        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="invocation"/> is an assertion call
        /// belonging to this framework.
        /// </summary>
        bool IsAssertionCall(InvocationExpressionSyntax invocation);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="ancestor"/> represents an assertion
        /// scope for this framework that encloses <paramref name="invocation"/>.
        /// </summary>
        bool IsAssertionScopeExpression(SyntaxNode ancestor, InvocationExpressionSyntax invocation);
    }
}
