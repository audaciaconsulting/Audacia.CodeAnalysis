using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNoExceptionsAsTemplateParameters
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogMessagesNoExceptionsAsTemplateParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.LogMessagesNoExceptionsAsTemplateParameters;

        private const string Title = "Log message template format contains an exception";
        private const string MessageFormat = "Log message property '{0}' is an exception";
        private const string Description = "Do not use exceptions as log message template parameters. These should use the correct overload of the logging method with an exception parameter.";

        private const string LoggerArgsParameterName = "args";

        private const string ExceptionTypeName = "System.Exception";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var invocation = (InvocationExpressionSyntax)nodeAnalysisContext.Node;

            if (!invocation.TryGetLoggerMethodSymbol(nodeAnalysisContext.SemanticModel, out var methodSymbol))
            {
                return;
            }

            // If 'args' is passed as an explicit named argument it will be a single array expression
            // rather than individual positional arguments — inspect the array initializer elements instead.
            if (invocation.TryGetExplicitlyNamedParameter(LoggerArgsParameterName, out var namedArgsArgument))
            {
                AnalyzeExplicitArgsArray(nodeAnalysisContext, namedArgsArgument);
                return;
            }

            // Find the paramIndex where the parameter is called 'args'
            var paramIndex = invocation.FindParameterIndex(LoggerArgsParameterName, methodSymbol);

            if (paramIndex == -1)
            {
                return;
            }

            // Args is always the last parameter, so all arguments from paramIndex onwards belong to it.
            foreach (var argument in invocation.ArgumentList.Arguments.Skip(paramIndex))
            {
                ReportIfException(nodeAnalysisContext, argument.Expression);
            }
        }

        /// <summary>
        /// Inspects the elements of an explicitly-passed args array (e.g. <c>args: new object[]{ ex, 1 }</c>)
        /// and reports a diagnostic for any element whose type is or inherits from <see cref="ExceptionTypeName"/>.
        /// </summary>
        private static void AnalyzeExplicitArgsArray(SyntaxNodeAnalysisContext context, ArgumentSyntax argsArgument)
        {
            // C# 12 collection expressions can be used to create arrays (e.g. args: [ ex, 1 ]).
            if (argsArgument.Expression is CollectionExpressionSyntax collectionExpression)
            {
                foreach (var element in collectionExpression.Elements.OfType<ExpressionElementSyntax>())
                {
                    ReportIfException(context, element.Expression);
                }
                return;
            }

            InitializerExpressionSyntax initializer = null;

            if (argsArgument.Expression is ArrayCreationExpressionSyntax arrayCreation)
            {
                initializer = arrayCreation.Initializer;
            }
            else if (argsArgument.Expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                initializer = implicitArrayCreation.Initializer;
            }

            if (initializer == null)
            {
                return;
            }

            foreach (var element in initializer.Expressions)
            {
                ReportIfException(context, element);
            }
        }

        private static void ReportIfException(SyntaxNodeAnalysisContext context, ExpressionSyntax element)
        {
            var elementType = context.SemanticModel.GetTypeInfo(element).Type;
            if (elementType != null && elementType.IsOrInheritsFrom(ExceptionTypeName))
            {
                var diagnostic = Diagnostic.Create(Rule, element.GetLocation(), element.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
