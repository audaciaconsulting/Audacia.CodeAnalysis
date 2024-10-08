using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.NestedControlStatements
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NestedControlStatementsAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.NestedControlStatements;

        public const int DefaultMaximumDepth = 2;

        private const string Title = "Signature contains too many nested control flow statements";

        private const string MessageFormat = "{0} contains {1} nested control flow statements, which exceeds the maximum of {2} nested control flow statements";

        private const string Description = "Don't nest too many control statements.";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        private static readonly SyntaxKind[] ControlStatementKinds =
        {
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.ForEachVariableStatement,
            SyntaxKind.IfStatement,
            SyntaxKind.ElseClause,
            SyntaxKind.SwitchExpression,
            SyntaxKind.SwitchStatement,
            SyntaxKind.TryStatement,
            SyntaxKind.CatchClause,
            SyntaxKind.FinallyClause
        };

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var settingsReader = new EditorConfigSettingsReader(startContext.Options);
            startContext.RegisterSyntaxNodeAction(actionContext => Analyze(actionContext, settingsReader), ControlStatementKinds);
        }

        private static int GetMaxDepth(SyntaxNodeAnalysisContext context, EditorConfigSettingsReader settingsReader)
        {
            var configValue = settingsReader.TryGetInt(context.Node.SyntaxTree, new SettingsKey(Id, "max_control_statement_depth"));

            return configValue ?? DefaultMaximumDepth;
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, EditorConfigSettingsReader settingsReader)
        {
            var max = GetMaxDepth(context, settingsReader);

            AnalyzeStatement(context, context.Node, max);
        }

        private static void AnalyzeStatement(SyntaxNodeAnalysisContext context, SyntaxNode node, int max, int depth = 1)
        {
            // we only want to analyze the first statement in nested statements, and not any nested statements too avoid duplicate results
            if (!node.Parent.IsKind(SyntaxKind.GlobalStatement) && !node.Parent.Parent.IsKind(SyntaxKind.MethodDeclaration) && !node.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
            {
                return;
            }

            var nodesTooDeep = GetNodesInViolation(node, max).ToList().Distinct();

            foreach (var deepNode in nodesTooDeep)
            {
                ReportAtContainingSymbol(deepNode.depth, max, context, deepNode.node);
            }
        }

        private static IEnumerable<(SyntaxNode node, int depth)> GetNodesInViolation(SyntaxNode node, int max, int depth = 1)
        {
            depth++;

            var blocks = node.ChildNodes()
                .Where(n => n.IsKind(SyntaxKind.Block) || n.IsKind(SyntaxKind.SwitchSection) || n.IsKind(SyntaxKind.SwitchExpressionArm))
                .ToList();

            foreach (var block in blocks)
            {
                var childStatements = block.ChildNodes().Where(n => ControlStatementKinds.Contains(n.Kind())).ToList();
                var ifStatements = block.ChildNodes().Where(n => n.IsKind(SyntaxKind.IfStatement)).ToList();

                if (ifStatements.Any())
                {
                    foreach (var internalNodes in ifStatements.Select(GetIfNodes))
                    {
                        childStatements.AddRange(internalNodes);
                    }
                }

                foreach (var child in childStatements)
                {
                    if (depth > max)
                    {
                        yield return (child, depth);
                    }

                    var deeperNodes = GetNodesInViolation(child, max, depth);

                    foreach (var deepNode in deeperNodes)
                    {
                        yield return deepNode;
                    }
                }
            }
        }

        private static List<SyntaxNode> GetIfNodes(SyntaxNode node)
        {
            var result = new List<SyntaxNode>();
            
            if (node is IfStatementSyntax ifStatement)
            {
                result.Add(ifStatement);
                
                if (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
                {
                    result.AddRange(GetIfNodes(elseIfStatement));
                }
                else if (ifStatement.Else != null)
                {
                    result.Add(ifStatement.Else);
                }
            }

            return result;
        }

        private static void ReportAtContainingSymbol(int depth, int maxDepth, SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            var baseKind = node.Kind();
            var kind = baseKind.ToString();
            var location = node.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, kind, depth, maxDepth));
        }
    }
}
