using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Core.Rules.UseInitForRecordSetters
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseInitForRecordSettersAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.UseInitForRecordSetters;
        public static DiagnosticSeverity Severity => DiagnosticSeverity.Warning;

        private const string Title = "Record property setter should be init-only";
        private const string MessageFormat = "Record property '{0}' in record type '{1}' should be init-only";
        private const string Description = "Record properties should use 'init' instead of 'set', to ensure immutability.";
        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Usage,
            Severity,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (!type.IsRecord)
            {
                return;
            }

            var properties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(HasSetAccessor)
                .Where(p => !p.IsStatic && !p.IsOverride && !p.IsIndexer)
                .Where(p => HasExplicitSetAccessor(p, context.CancellationToken))
                .Where(p => !ImplementsInterfaceSetter(type, p));

            foreach (var property in properties)
            {
                ReportDiagnostic(context, type, property);
            }
        }

        /// <summary>
        /// Excludes properties synthesized from positional parameters (e.g. on a positional record struct), which have no accessor syntax to fix.
        /// </summary>
        private static bool HasExplicitSetAccessor(IPropertySymbol property, CancellationToken cancellationToken)
        {
            return property.SetMethod.DeclaringSyntaxReferences
                .Any(reference => reference.GetSyntax(cancellationToken) is AccessorDeclarationSyntax);
        }

        /// <summary>
        /// Excludes properties implementing an interface property with a 'set' accessor, where replacing 'set' with 'init' would no longer satisfy the interface.
        /// </summary>
        private static bool ImplementsInterfaceSetter(INamedTypeSymbol type, IPropertySymbol property)
        {
            if (property.ExplicitInterfaceImplementations.Any(HasSetAccessor))
            {
                return true;
            }

            return type.AllInterfaces
                .SelectMany(i => i.GetMembers(property.Name).OfType<IPropertySymbol>())
                .Where(HasSetAccessor)
                .Any(member => SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), property));
        }

        private static bool HasSetAccessor(IPropertySymbol property)
        {
            return property.SetMethod != null && !property.SetMethod.IsInitOnly;
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, INamedTypeSymbol type, IPropertySymbol property)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                property.SetMethod.Locations.FirstOrDefault() ?? property.Locations[0],
                property.Name,
                type.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
