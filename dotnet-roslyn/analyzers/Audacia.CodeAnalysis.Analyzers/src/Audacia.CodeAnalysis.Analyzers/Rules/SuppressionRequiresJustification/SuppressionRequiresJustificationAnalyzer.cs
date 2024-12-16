using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.SuppressionRequiresJustification
{
    /// <summary>
    /// An analyzer which checks several attributes which suppress code analysis warnings/errors, validating that they have a justification.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuppressionRequiresJustificationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic Id.
        /// </summary>
        public const string Id = DiagnosticId.SuppressionRequiresJustification;

        /// <summary>
        /// The name of the argument which requires a value.
        /// </summary>
        public const string JustificationName = "Justification";

        /// <summary>
        /// The placeholder to insert as part of the code fix.
        /// </summary>
        public const string JustificationPlaceholder = "<Pending>";

        /// <summary>
        /// The title of the code analysis error.
        /// </summary>
        private const string Title = "Code analysis suppression attribute requires a justification";

        /// <summary>
        /// The format of message displayed to the user, with the first argument representing the attribute's name.
        /// </summary>
        private const string MessageFormat = "{0} is missing a value for 'Justification'";

        /// <summary>
        /// The description of this error.
        /// </summary>
        private const string Description = "Justification is required when using analysis suppression attributes.";

        /// <summary>
        /// A URL to the README.md heading for this analyzer, on GitHub.
        /// </summary>
        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        private static readonly Action<CompilationStartAnalysisContext> RegisterCompilationStartAction =
            RegisterCompilationStart;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(RegisterCompilationStartAction);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var instance = new AnalyzerInstance();
            startContext.RegisterSyntaxNodeAction(instance.HandleAttributeNode, SyntaxKind.Attribute);
        }

        private sealed class AnalyzerInstance
        {
            /// <summary>
            /// A lazily-initialized reference for <see cref="SuppressMessageAttribute"/>.
            /// </summary>
            private const string suppressMessageAttribute = "SuppressMessageAttribute";

            /// <summary>
            /// A lazily-initialized reference for <see cref="MaxMethodLengthAttribute"/>.
            /// </summary>
            private const string maxMethodLengthAttribute = "MaxMethodLengthAttribute";

            /// <summary>
            /// A lazily-initialized reference for <see cref="MaxParameterCountAttribute"/>.
            /// </summary>
            private string maxParameterCountAttribute = "MaxParameterCountAttribute";

            public AnalyzerInstance()
            {
            }

            /// <summary>
            /// Extracts the symbol of the current code and calls validation.
            /// </summary>
            /// <param name="context"></param>
            public void HandleAttributeNode(SyntaxNodeAnalysisContext context)
            {
                var attribute = (AttributeSyntax)context.Node;
                var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                if (symbol != null)
                {
                    ValidateAttribute(context, attribute, symbol);
                }
            }

            /// <summary>
            /// Loads the symbols for each of the subject attributes and checks that this <see cref="Attribute"/> matches.
            /// </summary>
            private void ValidateAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, ISymbol symbol)
            {
                if (IsMatchingSymbol(symbol))
                {
                    ValidateJustificationArgument(context, attribute, symbol);
                }
            }

            /// <summary>
            /// Loops through the arguments of the attribute. 
            /// </summary>
            /// <returns>
            /// A diagnostic error if
            /// <list type="bullet">
            /// <item>A justification is not found</item>
            /// <item>A justification is found, but contains an empty or <see cref="JustificationPlaceholder"/> value.</item>
            /// </list>
            /// </returns>
            private void ValidateJustificationArgument(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, ISymbol symbol)
            {
                foreach (var attributeArgument in attribute.ArgumentList.Arguments)
                {
                    if (attributeArgument.NameEquals?.Name?.Identifier.ValueText == JustificationName)
                    {
                        // Check if the justification is not empty
                        var value = context.SemanticModel.GetConstantValue(attributeArgument.Expression);

                        // If value does not have a value the expression is not constant -> Compilation error
                        if (!value.HasValue || (!string.IsNullOrWhiteSpace(value.Value as string) && (value.Value as string) != JustificationPlaceholder))
                        {
                            return;
                        }

                        // Empty, Whitespace, placeholder, or null justification provided
                        context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.GetLocation(), symbol.ContainingType.Name));
                        return;
                    }
                }

                // No justification set
                context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.GetLocation(), symbol.ContainingType.Name));
            }

            private bool IsMatchingSymbol(ISymbol symbol)
            {
                return  symbol.ContainingType.Name == suppressMessageAttribute ||
                        symbol.ContainingType.Name == maxMethodLengthAttribute ||
                        symbol.ContainingType.Name == maxParameterCountAttribute;
            }
        }
    }
}