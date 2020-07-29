using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MagicNumber
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MagicNumberAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ACL1001";

        private const string Title = "Variable declaration uses a magic number";
        private const string MessageFormat = "Variable declaration for '{0}' should not use a magic number.";
        private const string Description = "Variable declarations should not use a magic number. Move the number to a constant field with a descriptive name.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (LocalDeclarationStatementSyntax)context.Node;

            if (variableDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            foreach (var variable in variableDeclaration.Declaration.Variables)
            {
                var initializer = variable.Initializer;
                if (initializer == null)
                {
                    return;
                }

                if (initializer.Value is BinaryExpressionSyntax binaryExpression)
                {
                    AnalyzeBinaryExpression(context, variable, binaryExpression);
                }
            }
        }

        private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variable, BinaryExpressionSyntax binaryExpression)
        {
            AnalyzeExpression(binaryExpression.Left);
            AnalyzeExpression(binaryExpression.Right);

            // Local function used as want to keep the recursive call as close as possible
            void AnalyzeExpression(CSharpSyntaxNode syntaxNode)
            {
                if (syntaxNode is BinaryExpressionSyntax leftBinaryExpression)
                {
                    AnalyzeBinaryExpression(context, variable, leftBinaryExpression);
                }
                else if (syntaxNode.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, syntaxNode.GetLocation(), variable.Identifier.Text));
                }
            }
        }
    }
}
