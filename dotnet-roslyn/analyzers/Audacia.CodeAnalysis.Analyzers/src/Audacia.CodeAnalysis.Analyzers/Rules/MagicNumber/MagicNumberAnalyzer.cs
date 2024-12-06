using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
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
        public const string Id = DiagnosticId.MagicNumber;

        private const string Title = "Variable declaration uses a magic number";
        private const string MessageFormat = "Variable declaration for '{0}' should not use a magic number";
        private const string Description = "Variable declarations should not use a magic number. Move the number to a constant field with a descriptive name.";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationSyntax, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeIfSyntax, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(AnalyzeCaseSwitchLabelSyntax, SyntaxKind.CaseSwitchLabel);
            context.RegisterSyntaxNodeAction(AnalyzeSwitchStatementSyntax, SyntaxKind.SwitchStatement);
            context.RegisterSyntaxNodeAction(AnalyzeForSyntax, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzeWhileSyntax, SyntaxKind.WhileStatement);
        }
        
        private static void AnalyzeLocalDeclarationSyntax(SyntaxNodeAnalysisContext context)
        {
            var localDeclarationSyntax = (LocalDeclarationStatementSyntax)context.Node;

            if (localDeclarationSyntax.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return;
            }

            AnalyzeVariables(localDeclarationSyntax.Declaration.Variables, context, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeVariables(SeparatedSyntaxList<VariableDeclaratorSyntax> variables, SyntaxNodeAnalysisContext context, SyntaxKind kind)
        {
            SyntaxKind[] TypesToCheckNumericDeclarations = new SyntaxKind[] { SyntaxKind.ForStatement, SyntaxKind.IfStatement, SyntaxKind.WhileStatement };
            foreach (var variable in variables)
            {
                var initializer = variable.Initializer;
                if (initializer == null)
                {
                    return;
                }

                if (TypesToCheckNumericDeclarations.Any(type => type == kind) && initializer.Value is LiteralExpressionSyntax numericLiteralExpression)
                {
                    AnalyzeNumericLiteral(numericLiteralExpression, context, variable);
                }

                if (initializer.Value is BinaryExpressionSyntax binaryExpression)
                {
                    AnalyzeBinaryExpression(context, binaryExpression, variable);
                }
            }
        }

        private static void AnalyzeForSyntax(SyntaxNodeAnalysisContext context)
        {
            var forStatementSyntax = (ForStatementSyntax)context.Node;
            if (forStatementSyntax.Declaration.Variables != null)
            {
                AnalyzeVariables(forStatementSyntax.Declaration.Variables, context, SyntaxKind.ForStatement);
            }
        }

        private static void AnalyzeWhileSyntax(SyntaxNodeAnalysisContext context)
        {
            var whileStatementSyntax = (WhileStatementSyntax)context.Node;
            if(whileStatementSyntax.Condition is BinaryExpressionSyntax binarySyntax)
            {
                AnalyzeBinaryExpression(context, binarySyntax);
            }
            
        }

        private static void AnalyzeIfSyntax(SyntaxNodeAnalysisContext context)
        {
            var ifStatementSyntax = (IfStatementSyntax)context.Node;
            if (ifStatementSyntax.Condition is BinaryExpressionSyntax binarySyntax)
            {
                AnalyzeBinaryExpression(context, binarySyntax);
            }

        }

        private static void AnalyzeCaseSwitchLabelSyntax(SyntaxNodeAnalysisContext context)
        {
            var caseSwitchLabelSyntax = (CaseSwitchLabelSyntax)context.Node;
            if (caseSwitchLabelSyntax.Value is LiteralExpressionSyntax numericLiteral)
            {
                AnalyzeNumericLiteral(numericLiteral, context);
            }
        }

        private static void AnalyzeSwitchStatementSyntax(SyntaxNodeAnalysisContext context)
        {
            var switchStatementSyntax = (SwitchStatementSyntax)context.Node;
            if (switchStatementSyntax.Expression is LiteralExpressionSyntax numericLiteral)
            {
                AnalyzeNumericLiteral(numericLiteral, context);
            }
        }

        private static void AnalyzeNumericLiteral(CSharpSyntaxNode syntaxNode, SyntaxNodeAnalysisContext context,VariableDeclaratorSyntax variable = null)
        {
            var reportedIdentifier = variable == null ? context.Node.Kind().ToString() : variable.Identifier.Text;
            if (IsAcceptableInteger(syntaxNode))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, syntaxNode.GetLocation(), reportedIdentifier));
        }

        private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context,BinaryExpressionSyntax binaryExpression, VariableDeclaratorSyntax variable = null)
        {
            AnalyzeExpression(binaryExpression.Left);
            AnalyzeExpression(binaryExpression.Right);

            // Local function used as want to keep the recursive call as close as possible
            void AnalyzeExpression(CSharpSyntaxNode syntaxNode)
            {
                if (syntaxNode is BinaryExpressionSyntax leftBinaryExpression)
                {
                    AnalyzeBinaryExpression(context, leftBinaryExpression, variable);
                }
                else if (syntaxNode.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    AnalyzeNumericLiteral(syntaxNode, context, variable);
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
