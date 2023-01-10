using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MaximumWhereClauses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MaximumWhereClausesAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.MaximumWhereClausesAnalyzer;

        public const int DefaultMaximumClauses = 3;

        private const string Title = "'Where' method contains too many clauses, consisder separating out into separate chained 'Where' methods.";

        private const string MessageFormat = "'Where' contains {0} clauses, which exceeds the maximum of {1} clauses per 'Where'.";

        private const string Description = "Don't pass predicates into 'Where' methosd with too many clauses.";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            true,
            Description);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var settingsReader = new EditorConfigSettingsReader(startContext.Options);
            startContext.RegisterSyntaxNodeAction(actionContext => Analyze(actionContext, settingsReader), SyntaxKind.InvocationExpression);
        }

        private static int GetMaxClauses(SyntaxNodeAnalysisContext context, EditorConfigSettingsReader settingsReader)
        {
            var configValue = settingsReader.TryGetInt(context.Node.SyntaxTree, new SettingsKey(Id, "max_where_clauses"));

            return configValue ?? DefaultMaximumClauses;
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, EditorConfigSettingsReader settingsReader)
        {
            var max = GetMaxClauses(context, settingsReader);

            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var text = invocationExpression.GetText().ToString();

            // Check if invocation expression contains Where string.
            if (!text.Contains("Where") && !text.Contains("&&"))
            {
                return;
            }

            var ands = invocationExpression
                .DescendantNodes(n => n.IsKind(SyntaxKind.InvocationExpression) || n.IsKind(SyntaxKind.Argument) || n.IsKind(SyntaxKind.ArgumentList) || n.IsKind(SyntaxKind.SimpleLambdaExpression), true)
                .SelectMany(p => p.DescendantNodes(n => n.IsKind(SyntaxKind.LogicalAndExpression)))
                .ToList();

            // the number of ands counted above counts each '&&' symbol twice for some reason
            // so we want the number of '&&'s and then + 1 for the number of clauses
            var numberOfClauses = ands.Count() / 2 + 1;

            if (numberOfClauses > max)
            {
                ReportAtContainingSymbol(max, numberOfClauses, context, context.Node);
            }
        }

        private static void ReportAtContainingSymbol(int max, int clausesCount, SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            var location = node.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, clausesCount, max));
        }
    }
}
