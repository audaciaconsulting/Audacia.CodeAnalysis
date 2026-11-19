using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.IncludeBraces;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IncludeBracesAnalyzer : DiagnosticAnalyzer
{
    public const string Id = DiagnosticId.IncludeBraces;
    private const string Title = "Code block does not have braces";
    private const string MessageFormat = "Code block should have braces. CS-01.15.";
    private const string Description = "Add braces.";
    private const string Category = DiagnosticCategory.Maintainability;

    private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        true,
        Description,
        HelpLinkUrl);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
        context.RegisterSyntaxNodeAction(AnalyzeCommonForEachStatement, SyntaxKind.ForEachStatement);
        context.RegisterSyntaxNodeAction(AnalyzeCommonForEachStatement, SyntaxKind.ForEachVariableStatement);
        context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
        context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
        context.RegisterSyntaxNodeAction(AnalyzeWhileStatement, SyntaxKind.WhileStatement);
        context.RegisterSyntaxNodeAction(AnalyzeDoStatement, SyntaxKind.DoStatement);
        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        context.RegisterSyntaxNodeAction(AnalyzeFixedStatement, SyntaxKind.FixedStatement);
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
