using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

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
            if (!receiverType.IsOrImplementsInterface(LoggerTypeName))
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
            var pattern = isInterpolated ? NamedPropertyPatternInterpolated : NamedPropertyPattern;

            var allNamedProperties = Regex.Matches(messageParameterValue, pattern);

            // Track which property names have been seen (case-insensitive).
            // Every occurrence after the first is a duplicate and gets its own diagnostic.
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match namedProperty in allNamedProperties)
            {
                var propertyName = namedProperty.Groups[1].Value;

                if (!seenNames.Add(propertyName))
                {
                    var matchStart = messageArgument.SpanStart + namedProperty.Index;
                    var matchSpan = new TextSpan(matchStart, namedProperty.Length);
                    var location = Location.Create(nodeAnalysisContext.Node.SyntaxTree, matchSpan);

                    nodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, location, propertyName));
                }
            }
        }
    }
}