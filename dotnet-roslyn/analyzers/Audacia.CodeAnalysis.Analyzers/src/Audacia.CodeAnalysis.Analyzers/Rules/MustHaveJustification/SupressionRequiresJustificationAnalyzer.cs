using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MustHaveJustification
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SupressionRequiresJustificationAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.SupressionMustHaveJustification;
        
        /// <summary>
        /// The placeholder to insert as part of the code fix.
        /// </summary>
        public const string JustificationPlaceholder = "<Pending>";

        /// <summary>
        /// The Title of the 
        /// </summary>
        private const string Title = "Code analysis supression attribute requires a justification";

        private const string MessageFormat = "{0} is missing a value for 'Justification'";

        private const string Description = "Justification is required when using analysis supression attributes.";

        private const string JustificationArguementName = "Justification";

        private static ImmutableArray<Type> RelevantAttributes => ImmutableArray.Create(
            typeof(SuppressMessageAttribute), 
            typeof(MaxMethodLengthAttribute), 
            typeof(MaxParameterCountAttribute));

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
            var instance = new AnalyzerInstance(startContext.Compilation.GetOrCreateUsingAliasCache());
            startContext.RegisterSyntaxNodeAction(instance.HandleAttributeNode, SyntaxKind.Attribute);
        }

        private static Location GetMemberLocation(ISymbol member, SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            foreach (var arrowExpressionClause in member.DeclaringSyntaxReferences
                         .Select(reference => reference.GetSyntax(cancellationToken))
                         .OfType<ArrowExpressionClauseSyntax>())
            {
                var parentSymbol = semanticModel.GetDeclaredSymbol(arrowExpressionClause.Parent);

                if (parentSymbol != null && parentSymbol.Locations.Any())
                {
                    return parentSymbol.Locations[0];
                }
            }

            return member.Locations[0];
        }

        private sealed class AnalyzerInstance
        {
            private readonly ConcurrentDictionary<SyntaxTree, bool> _usingAliasCache;

            private INamedTypeSymbol suppressMessageAttribute;
            private INamedTypeSymbol maxMethodLengthAttribute;
            private INamedTypeSymbol maxParameterCountAttribute;

            public AnalyzerInstance(ConcurrentDictionary<SyntaxTree, bool> usingAliasCache)
            {
                _usingAliasCache = usingAliasCache;
            }

            public void HandleAttributeNode(SyntaxNodeAnalysisContext context)
            {
                var attribute = (AttributeSyntax)context.Node;
                var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                if (symbol != null)
                {
                    ValidateRelevantAttribute(context, attribute, symbol);
                }
            }

            private void ValidateRelevantAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, ISymbol symbol)
            {
                if (suppressMessageAttribute == null)
                {
                    suppressMessageAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName);
                }

                if (maxMethodLengthAttribute == null)
                { 
                    maxMethodLengthAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(MaxMethodLengthAttribute).FullName);
                }

                if (maxParameterCountAttribute == null)
                {
                    maxParameterCountAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(MaxParameterCountAttribute).FullName);
                }

                if (IsMatchingSymbol(symbol))
                {
                    ValidateJustificationArgument(context, attribute, symbol);
                }
            }

            private void ValidateJustificationArgument(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, ISymbol symbol)
            {
                foreach (var attributeArgument in attribute.ArgumentList.Arguments)
                {
                    if (attributeArgument.NameEquals?.Name?.Identifier.ValueText == JustificationArguementName)
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
                return SymbolEqualityComparer.Default.Equals(symbol.ContainingType, suppressMessageAttribute) ||
                    SymbolEqualityComparer.Default.Equals(symbol.ContainingType, maxMethodLengthAttribute) ||
                    SymbolEqualityComparer.Default.Equals(symbol.ContainingType, maxParameterCountAttribute);
            }

            private bool IsAliasOfRelevantAttribute(AttributeSyntax attribute)
            {

                if (!(attribute.Name is SimpleNameSyntax simpleNameSyntax))
                {
                    if (attribute.Name is AliasQualifiedNameSyntax aliasQualifiedNameSyntax)
                    {
                        simpleNameSyntax = aliasQualifiedNameSyntax.Name;
                    }
                    else
                    {
                        var qualifiedNameSyntax = attribute.Name as QualifiedNameSyntax;
                        simpleNameSyntax = qualifiedNameSyntax.Right;
                    }
                }

                var isAliasOfAttribute = RelevantAttributes.Any(relevantAttribute => relevantAttribute.Name == simpleNameSyntax.Identifier.ValueText);

                return isAliasOfAttribute;
            }
        }
    }
}