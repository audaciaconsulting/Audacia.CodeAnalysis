using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MagicNumber
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MagicNumberAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.MagicNumber;

        private const string Title = "Variable declaration uses a magic number";
        private const string MessageFormat = "Variable declaration for '{0}' should not use a magic number.";
        private const string Description = "Variable declarations should not use a magic number. Move the number to a constant field with a descriptive name.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
                    if (IsAcceptableInteger(syntaxNode))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Rule, syntaxNode.GetLocation(), variable.Identifier.Text));
                }
            }
        }

        private static bool IsAcceptableInteger(CSharpSyntaxNode syntaxNode)
        {
            var literal = (LiteralExpressionSyntax)syntaxNode;
            if (literal.Token.Value is int integer)
            {
                // No diagnostic if it's a literal 0 or 1, or a multiple of 10
                // 0 is a often used as a literal e.g. with the null coalescing operator
                // 1 is a special case as it's common in code to increment/decrement by 1 so this should also be allowed
                // Multiples of 10 are often used to convert between units (e.g. pounds/pence, grams/kilograms) or to/from percentages
                // and it's usually clear from context that this is happening

                return integer == 0 || integer == 1 || (integer % 10 == 0);
            }

            return false;
        }
    }
}
