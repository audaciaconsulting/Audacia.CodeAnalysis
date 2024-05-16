using System;
using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.FieldWithUnderscore
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FieldWithUnderscoreAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.FieldWithUnderscore;

        private const string Title = "Private field not prefixed with an underscore";
        private const string MessageFormat = "Field '{0}' is not prefixed with an underscore";
        private const string Description = "Private fields should be prefixed with an underscore.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Naming, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;
            if (!ShouldAnalyze(field))
            {
                return;
            }

            if (!field.Name.StartsWith("_", StringComparison.Ordinal))
            {
                var diagnostic = Diagnostic.Create(Rule, field.Locations[0], field.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool ShouldAnalyze(IFieldSymbol field)
        {
            if (field.IsStatic && field.IsReadOnly)
            {
                return false;
            }

            if (field.IsConst)
            {
                return false;
            }

            if (field.DeclaredAccessibility == Accessibility.Public ||
                field.DeclaredAccessibility == Accessibility.Internal)
            {
                return false;
            }

            return true;
        }
    }
}
