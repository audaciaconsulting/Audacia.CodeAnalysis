using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNamedPropertiesPascalCase
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogMessagesNamedPropertiesPascalCaseAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.LogMessagesNamedPropertiesPascalCase;

        private const string Title = "Log message property is not in PascalCase";
        private const string MessageFormat = "Log message property '{0}' does not use PascalCase";
        private const string Description = "When using log message named properties, ensure they are in PascalCase.";

        private const string InvalidNamedPropertyPattern = @"(?<!\{)\{(?!\{)(?!@?[A-Z][a-zA-Z0-9]*[}:])([^{}:]+)[^{}]*\}(?!\})";

        // In interpolated strings, log template placeholders are double-braced in source: {{Name}} => {Name} at runtime.
        // This pattern matches {{name}} but not {{{{...}}}} (which are escaped literal braces).
        private const string InvalidNamedPropertyPatternInterpolated = @"(?<!\{)\{\{(?!\{)(?!@?[A-Z][a-zA-Z0-9]*[}:])([^{}:]+)[^{}]*\}\}(?!\})";

        private const string LoggerTypeName = "Microsoft.Extensions.Logging.ILogger";
        private const string LoggerMessageParameterName = "message";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Naming, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
            if (!IsOrImplementsILogger(receiverType))
            {
                return;
            }

            // Find the paramIndex where the parameter is called 'message'
            var paramIndex = invocation.FindParameterIndex(LoggerMessageParameterName, methodSymbol);

            if (paramIndex == -1)
            {
                return;
            }

            var messageArgument = invocation.ArgumentList.Arguments[paramIndex];
            var messageParameterValue = messageArgument.ToString();

            var isInterpolated = messageArgument.Expression is InterpolatedStringExpressionSyntax;
            var pattern = isInterpolated ? InvalidNamedPropertyPatternInterpolated : InvalidNamedPropertyPattern;

            // Use regex to find all named properties in the message template that do not follow PascalCase
            var invalidNamedProperties = Regex.Matches(messageParameterValue, pattern);

            foreach (Match namedProperty in invalidNamedProperties)
            {
                var propertyName = namedProperty.Groups[1].Value;

                // Set the diagnostic location to the position of the named property with the correct row and column
                var matchStart = messageArgument.SpanStart + namedProperty.Index;
                var matchSpan = new TextSpan(matchStart, namedProperty.Length);
                var location = Location.Create(nodeAnalysisContext.Node.SyntaxTree, matchSpan);

                var diagnostic = Diagnostic.Create(Rule, location, propertyName);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsOrImplementsILogger(ITypeSymbol typeSymbol)
        {
            if(typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.ToDisplayString().Equals(LoggerTypeName, System.StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (iface.ToDisplayString().Equals(LoggerTypeName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
