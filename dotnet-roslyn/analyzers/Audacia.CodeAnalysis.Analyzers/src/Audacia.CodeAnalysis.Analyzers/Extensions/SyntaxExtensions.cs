using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Audacia.CodeAnalysis.Analyzers.Common.AssertionFrameworks;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    /// <summary>
    /// Extensions to the various subclasses of <see cref="SyntaxNode"/>.
    /// </summary>
    internal static class SyntaxExtensions
    {
        private static readonly IAssertionFramework[] AssertionFrameworks =
        {
            new XunitAssertions(),
            new FluentAssertions(),
            new ShouldlyAssertions()
        };

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
            if (identifierName.Identifier.ValueText != nameof(ArgumentNullException))
            {
                return false;
            }

            return memberExpression.Name.Identifier.Text == "ThrowIfNull";
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

        /// <summary>
        /// Returns the simple (unqualified) method name from an invocation expression, handling
        /// plain identifiers, member-access expressions, and generic method calls.
        /// Returns <see langword="null"/> when the expression form is not recognised.
        /// The following examples will return <c>ShouldBe</c>:
        /// <list type="bullet">
        ///     <item><c>result.ShouldBe(42)</c></item>
        ///     <item><c>ShouldBe(42)</c></item>
        ///     <item><c>result.ShouldBe&lt;IFoo&gt;()</c></item>
        ///     <item><c>result?.ShouldBe(42)</c></item>
        /// </list>
        /// </summary>
        /// <param name="invocation">The invocation whose method name should be resolved.</param>
        internal static string GetSimpleMethodName(this InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.ValueText;
            }

            if (invocation.Expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText;
            }

            if (invocation.Expression is GenericNameSyntax generic)
            {
                return generic.Identifier.ValueText;
            }

            if (invocation.Expression is MemberBindingExpressionSyntax memberBinding)
            {
                return memberBinding.Name.Identifier.ValueText;
            }

            return null;
        }

        /// <summary>
        /// Returns the <see cref="IAssertionFramework"/> that recognises <paramref name="invocation"/> as one
        /// of its assertion calls, or <see langword="null"/> if the invocation is not a known assertion.
        /// </summary>
        internal static IAssertionFramework GetAssertionFramework(this InvocationExpressionSyntax invocation)
        {

            return AssertionFrameworks.FirstOrDefault(framework => framework.IsAssertionCall(invocation));
        }

        /// <summary>
        /// Returns the <see cref="IAssertionFramework"/> that recognises <paramref name="invocation"/> as being
        /// nested inside one of its assertion scopes, or <see langword="null"/> if the invocation is not nested inside any known assertion scope.
        /// </summary>
        internal static IAssertionFramework GetAssertionScopeFramework(this InvocationExpressionSyntax invocation)
        {
            return AssertionFrameworks.FirstOrDefault(framework => 
                invocation.Ancestors().Any(ancestor => framework.IsAssertionScopeExpression(ancestor, invocation)));
        }

        /// <summary>
        /// Uses the semantic model to resolve <paramref name="invocation"/> to the <see cref="MethodDeclarationSyntax"/>
        /// declared in the same compilation, or returns <see langword="null"/> if not found.
        /// </summary>
        internal static MethodDeclarationSyntax ResolveHelperMethodDeclaration(this InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            var symbol = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

            if (symbol == null)
            {
                return null;
            }

            var methodDeclaration = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
            return methodDeclaration;
        }
    }
}
