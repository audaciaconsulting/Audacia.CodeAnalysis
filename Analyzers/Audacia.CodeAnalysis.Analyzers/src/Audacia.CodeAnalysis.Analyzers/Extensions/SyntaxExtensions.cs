using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    /// <summary>
    /// Extensions to the various subclasses of <see cref="SyntaxNode"/>.
    /// </summary>
    internal static class SyntaxExtensions
    {
        /// <summary>
        /// Checks whether the given <paramref name="statementSyntax"/> represents an argument null check,
        /// i.e. a construct like this:
        /// <code>
        /// if (arg == null) throw new ArgumentNullException(nameof(arg));
        /// </code>
        /// </summary>
        /// <param name="statementSyntax"></param>
        internal static bool IsArgumentNullCheck(this StatementSyntax statementSyntax)
        {
            // This method will get called for the 'if' statement checking the argument for null
            // and for the 'throw' statement actually throwing the exception.
            // We want to exclude both from the statement count, therefore we need to check both eventualities.

            if (statementSyntax.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax)statementSyntax;

                return ifStatement.IsSingleStatement() && ifStatement.ContainsThrowArgumentNullExceptionStatement();
            }

            if (statementSyntax.IsKind(SyntaxKind.ThrowStatement))
            {
                // If it's a throw statement, we need to find the parent 'if' statement to ensure it's just a single statement.
                // Either the direct parent is an 'if' (no enclosing quotes), or the parent is a code block, and it's parent is the 'if'.
                // If neither is true then the throw is in some other part of the code.
                IfStatementSyntax parentIfStatement = null;
                if (statementSyntax.Parent.IsKind(SyntaxKind.IfStatement))
                {
                    parentIfStatement = (IfStatementSyntax)statementSyntax.Parent;
                }
                else if (statementSyntax.Parent.IsKind(SyntaxKind.Block) &&
                         statementSyntax.Parent.Parent.IsKind(SyntaxKind.IfStatement))
                {
                    parentIfStatement = (IfStatementSyntax)statementSyntax.Parent.Parent;
                }

                if (parentIfStatement == null)
                {
                    return false;
                }

                var throwStatement = (ThrowStatementSyntax)statementSyntax;

                return throwStatement.IsThrowArgumentNullExceptionStatement() && parentIfStatement.IsSingleStatement();
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given <paramref name="ifStatementSyntax"/> contains a single statement.
        /// </summary>
        /// <param name="ifStatementSyntax"></param>
        internal static bool IsSingleStatement(this IfStatementSyntax ifStatementSyntax)
        {
            if (!ifStatementSyntax.Statement.IsKind(SyntaxKind.Block))
            {
                // If the 'if' statement's statement is not a block (i.e. no braces) then it must be a single statement
                return true;
            }

            var blockStatement = (BlockSyntax)ifStatementSyntax.Statement;

            return blockStatement.Statements.Count == 1;
        }

        /// <summary>
        /// Checks whether the given <paramref name="ifStatementSyntax"/> contains a statement throwing an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="ifStatementSyntax"></param>
        internal static bool ContainsThrowArgumentNullExceptionStatement(this IfStatementSyntax ifStatementSyntax)
        {
            var innerStatement = ifStatementSyntax.Statement;
            if (!innerStatement.IsKind(SyntaxKind.Block) &&
                !innerStatement.IsKind(SyntaxKind.ThrowStatement))
            {
                // If it's not a block (i.e. braces) then it should be a throw.
                // Anything else means an 'if' statement with no braces and some non-throw statement.
                return false;
            }

            ThrowStatementSyntax throwStatement;
            if (innerStatement.IsKind(SyntaxKind.Block))
            {
                // A block could contain multiple throws, so just check if any are throwing argument null.
                var blockStatement = (BlockSyntax)innerStatement;
                var throwStatements = blockStatement.Statements.OfType<ThrowStatementSyntax>();

                return throwStatements.Any(statement => statement.IsThrowArgumentNullExceptionStatement());
            }
            
            // If we get here it must be a throw statement.
            throwStatement = (ThrowStatementSyntax)innerStatement;

            return throwStatement.IsThrowArgumentNullExceptionStatement();
        }

        /// <summary>
        /// Checks if the given <paramref name="throwStatementSyntax"/> is throwing an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="throwStatementSyntax"></param>
        internal static bool IsThrowArgumentNullExceptionStatement(this ThrowStatementSyntax throwStatementSyntax)
        {
            if (!throwStatementSyntax.Expression.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                return false;
            }

            var creationStatement = (ObjectCreationExpressionSyntax)throwStatementSyntax.Expression;
            if (!creationStatement.Type.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            var identifierName = (IdentifierNameSyntax)creationStatement.Type;

            return identifierName.Identifier.ValueText == nameof(ArgumentNullException);
        }
    }
}
