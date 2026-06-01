using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNoDuplicateParameters
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LogMessagesNoDuplicateParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.LogMessagesNoDuplicateParameters;

        private const string Title = "Log message property is duplicated";
        private const string MessageFormat = "Log message property '{0}' is duplicated";
        private const string Description = "When using log message named properties ensure they are unique within a message template.";

        // Matches named (non-positional) log message properties in regular strings, capturing the property name in group 1.
        // Excludes escaped braces ({{...}}) and positional placeholders ({0}, {1:N0}).
        private const string NamedPropertyPattern = @"(?<!\{)\{(?!\{)(@?\D[^:{}]*)(?:[^{}]*)?\}(?!\})";

        // In interpolated strings, log template placeholders are double-braced in source: {{Name}} => {Name} at runtime.
        // This pattern matches {{name}} but not {{{{...}}}} (which are escaped literal braces).
        private const string NamedPropertyPatternInterpolated = @"(?<!\{)\{\{(?!\{)(@?\D[^:{}]*)(?:[^{}]*)?\}\}(?!\})";

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

            var messageParameterValue = messageArgument.ToString();
            var pattern = messageArgument.Expression is InterpolatedStringExpressionSyntax
                ? NamedPropertyPatternInterpolated
                : NamedPropertyPattern;

            var allNamedProperties = Regex.Matches(messageParameterValue, pattern);

            // Track which property names have been seen (case-insensitive).
            // Every occurrence after the first is a duplicate and gets its own diagnostic.
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match namedProperty in allNamedProperties)
            {
                var propertyName = namedProperty.Groups[1].Value;

                if (!seenNames.Add(propertyName))
                {
                    var location = messageArgument.CreateMatchLocation(nodeAnalysisContext.Node.SyntaxTree, namedProperty);
                    nodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, location, propertyName));
                }
            }
        }
    }
}