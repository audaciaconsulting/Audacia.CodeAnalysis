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

        private const string LoggerTypeName = "Microsoft.Extensions.Logging.ILogger";
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

            var semanticModel = nodeAnalysisContext.SemanticModel;

            // Resolve the method symbol for the invocation
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            var receiverType = methodSymbol.ReceiverType;
            // Check if the method is called on a type that is, or implements, Microsoft.Extensions.Logging.ILogger
            if (!receiverType.IsOrImplementsInterface(LoggerTypeName))
            {
                return;
            }

            // Find the paramIndex where the parameter is called 'args'
            var paramIndex = invocation.FindParameterIndex(LoggerArgsParameterName, methodSymbol);

            if (paramIndex == -1)
            {
                return;
            }

            // Check if any of the arguments passed to the 'args' parameter is or inherits from System.Exception
            // Args is always the last parameter, so we can check all arguments from paramIndex to the end of the argument list
            foreach (var argument in invocation.ArgumentList.Arguments.Skip(paramIndex))
            {
                var argumentType = semanticModel.GetTypeInfo(argument.Expression).Type;
                if (argumentType != null && argumentType.IsOrInheritsFrom(ExceptionTypeName))
                {
                    // Get the name of the log message property from the argument expression
                    var propertyName = argument.Expression.ToString();
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), propertyName);
                    nodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
