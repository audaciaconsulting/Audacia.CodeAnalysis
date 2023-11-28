using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.IncludeBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IncludeBracesAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.IncludeBraces;
        private const string Title = "Code block does not have braces";
        private const string MessageFormat = "Code block should have braces.";
        private const string Description = "Add braces.";

        private const string Category = DiagnosticCategory.Style;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(f => AnalyzeIfStatement(f), SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeElseClause(f), SyntaxKind.ElseClause);
            context.RegisterSyntaxNodeAction(f => AnalyzeCommonForEachStatement(f), SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeCommonForEachStatement(f), SyntaxKind.ForEachVariableStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeForStatement(f), SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeUsingStatement(f), SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeWhileStatement(f), SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeDoStatement(f), SyntaxKind.DoStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeLockStatement(f), SyntaxKind.LockStatement);
            context.RegisterSyntaxNodeAction(f => AnalyzeFixedStatement(f), SyntaxKind.FixedStatement);
        }

        private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (ifStatement.IsArgumentNullCheck())
            {
                return;
            }

            StatementSyntax statement = ifStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeElseClause(SyntaxNodeAnalysisContext context)
        {
            var elseClause = (ElseClauseSyntax)context.Node;

            StatementSyntax statement = elseClause.EmbeddedStatement(allowIfStatement: false);

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeCommonForEachStatement(SyntaxNodeAnalysisContext context)
        {
            var forEachStatement = (CommonForEachStatementSyntax)context.Node;

            StatementSyntax statement = forEachStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var forStatement = (ForStatementSyntax)context.Node;

            StatementSyntax statement = forStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            StatementSyntax statement = usingStatement.EmbeddedStatement(allowUsingStatement: false);

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeWhileStatement(SyntaxNodeAnalysisContext context)
        {
            var whileStatement = (WhileStatementSyntax)context.Node;

            StatementSyntax statement = whileStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
        {
            var doStatement = (DoStatementSyntax)context.Node;

            StatementSyntax statement = doStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatement = (LockStatementSyntax)context.Node;

            StatementSyntax statement = lockStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }

        private static void AnalyzeFixedStatement(SyntaxNodeAnalysisContext context)
        {
            var fixedStatement = (FixedStatementSyntax)context.Node;

            StatementSyntax statement = fixedStatement.EmbeddedStatement();

            if (statement == null)
            {
                return;
            }

            if (statement.ContainsDirectives)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
        }
    }
}
