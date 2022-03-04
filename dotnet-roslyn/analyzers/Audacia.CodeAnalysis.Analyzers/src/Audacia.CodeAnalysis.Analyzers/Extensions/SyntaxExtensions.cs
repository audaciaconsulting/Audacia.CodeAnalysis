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

                return ifStatement.IsArgumentNullCheck();
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

            if (statementSyntax.IsKind(SyntaxKind.ExpressionStatement))
            {
                var expressionStatement = (ExpressionStatementSyntax)statementSyntax;
                return expressionStatement.IsArgumentNullExceptionThrowIfNullExpression();
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given <paramref name="expressionStatement"/> represents <see cref="ArgumentNullException"/> throwing if null.
        /// i.e. like below.
        /// <code>
        /// ArgumentNullException.ThrowIfNull(obj);
        /// </code>
        /// </summary>
        /// <param name="expressionStatement">The </param>
        private static bool IsArgumentNullExceptionThrowIfNullExpression(this ExpressionStatementSyntax expressionStatement)
        {
            if (!expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression))
            {
                return false;
            }

            var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;

            if (!invocationExpression.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            var memberExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            if (!memberExpression.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            var identifierName = (IdentifierNameSyntax)memberExpression.Expression;
            return identifierName.Identifier.ValueText == nameof(ArgumentNullException);
        }

        /// <summary>
        /// Checks whether the given <paramref name="ifStatementSyntax"/> represents an argument null check,
        /// i.e. a construct like this:
        /// <code>
        /// if (arg == null) throw new ArgumentNullException(nameof(arg));
        /// </code>
        /// </summary>
        /// <param name="ifStatementSyntax"></param>
        internal static bool IsArgumentNullCheck(this IfStatementSyntax ifStatementSyntax)
        {
            return ifStatementSyntax.IsSingleStatement() && ifStatementSyntax.ContainsThrowArgumentNullExceptionStatement();
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

        internal static StatementSyntax EmbeddedStatement(this CommonForEachStatementSyntax forEachStatement)
        {
            StatementSyntax statement = forEachStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this DoStatementSyntax doStatement)
        {
            StatementSyntax statement = doStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this FixedStatementSyntax fixedStatement)
        {
            StatementSyntax statement = fixedStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this ForStatementSyntax forStatement)
        {
            StatementSyntax statement = forStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this IfStatementSyntax ifStatement)
        {
            StatementSyntax statement = ifStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this LockStatementSyntax lockStatement)
        {
            StatementSyntax statement = lockStatement.Statement;

            return statement.IfNotBlock();
        }

        internal static StatementSyntax EmbeddedStatement(this WhileStatementSyntax whileStatement)
        {
            StatementSyntax statement = whileStatement.Statement;

            return statement.IfNotBlock();
        }

        private static StatementSyntax IfNotBlock(this StatementSyntax statement) =>
            (statement?.Kind() == SyntaxKind.Block) ? null : statement;

        internal static StatementSyntax EmbeddedStatement(this UsingStatementSyntax usingStatement, bool allowUsingStatement = true)
        {
            StatementSyntax statement = usingStatement.Statement;

            if (statement == null)
            {
                return null;
            }

            SyntaxKind kind = statement.Kind();

            if (kind == SyntaxKind.Block)
            {
                return null;
            }

            if (!allowUsingStatement && kind == SyntaxKind.UsingStatement)
            {
                return null;
            }

            return statement;
        }

        internal static StatementSyntax EmbeddedStatement(this ElseClauseSyntax elseClause, bool allowIfStatement = true)
        {
            StatementSyntax statement = elseClause.Statement;

            if (statement == null)
            {
                return null;
            }

            SyntaxKind kind = statement.Kind();

            if (kind == SyntaxKind.Block)
            {
                return null;
            }

            if (!allowIfStatement && kind == SyntaxKind.IfStatement)
            {
                return null;
            }

            return statement;
        }
    }
}
