using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNamedPropertiesMustBeUsed
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogMessagesNamedPropertiesMustBeUsedAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.LogMessagesNamedPropertiesMustBeUsed;

        private const string Title = "Log message template format uses positional parameters";
        private const string MessageFormat = "Log message property '{0}' is a positional parameter";
        private const string Description = "Do not use positional parameters in log message templates. Use named properties instead.";

        private const string InvalidPositionalPropertyPattern = @"(?<!\{)\{(?!\{)(@?\d+(?=[}:,])[^{}]*)\}(?!\})";

        private const string InvalidPositionalPropertyPatternInterpolated = @"(?<!\{)\{\{(?!\{)(@?\d+(?=[}:,])[^{}]*)\}\}(?!\})";

        private const string LoggerMessageParameterName = "message";

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

            if (!invocation.TryGetNamedParameterArgument(methodSymbol, LoggerMessageParameterName, out var messageArgument))
            {
                return;
            }

            var messageParameterValue = messageArgument.ToString();
            var pattern = messageArgument.Expression is InterpolatedStringExpressionSyntax
                ? InvalidPositionalPropertyPatternInterpolated
                : InvalidPositionalPropertyPattern;

            // Use regex to find all named properties which are positional identifiers
            var invalidNamedProperties = Regex.Matches(messageParameterValue, pattern);

            foreach (Match namedProperty in invalidNamedProperties)
            {
                var propertyName = namedProperty.Groups[1].Value;
                var location = messageArgument.CreateMatchLocation(nodeAnalysisContext.Node.SyntaxTree, namedProperty);
                var diagnostic = Diagnostic.Create(Rule, location, propertyName);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
