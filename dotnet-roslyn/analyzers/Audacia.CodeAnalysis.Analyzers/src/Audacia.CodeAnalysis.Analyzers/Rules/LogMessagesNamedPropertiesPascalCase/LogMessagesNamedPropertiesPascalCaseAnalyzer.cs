using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

        private const string InvalidNamedPropertyPattern = @"(?<!\{)\{(?!\{)(?!(?:\d+|@?[A-Z][a-zA-Z0-9]*)[}:])(?'propertyName'[^{}:]+)[^{}]*\}(?!\})";

        // In interpolated strings, log template placeholders are double-braced in source: {{Name}} => {Name} at runtime.
        // This pattern matches {{name}} but not {{{{...}}}} (which are escaped literal braces).
        private const string InvalidNamedPropertyPatternInterpolated = @"(?<!\{)\{\{(?!\{)(?!(?:\d+|@?[A-Z][a-zA-Z0-9]*)[}:])(?'propertyName'[^{}:]+)[^{}]*\}\}(?!\})";
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

            if (!invocation.TryGetLoggerMethodSymbol(nodeAnalysisContext.SemanticModel, out var methodSymbol))
            {
                return;
            }

            if (!invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageParameterName, out var messageArgument))
            {
                return;
            }

            // If the argument is a local variable, try to resolve it back to its initializer string literal.
            var messageExpression = messageArgument.Expression.TryResolveToStringLiteral(nodeAnalysisContext.SemanticModel, out var resolvedExpression)
                ? resolvedExpression
                : messageArgument.Expression;

            var messageParameterValue = messageExpression.ToString();
            var pattern = messageExpression is InterpolatedStringExpressionSyntax
                ? InvalidNamedPropertyPatternInterpolated
                : InvalidNamedPropertyPattern;

            // Use regex to find all named properties in the message template that do not follow PascalCase
            var invalidNamedProperties = Regex.Matches(messageParameterValue, pattern);

            foreach (Match namedProperty in invalidNamedProperties)
            {
                var propertyName = namedProperty.Groups["propertyName"].Value;
                var location = messageExpression.CreateMatchLocation(messageExpression.SyntaxTree, namedProperty);
                var diagnostic = Diagnostic.Create(Rule, location, propertyName);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
