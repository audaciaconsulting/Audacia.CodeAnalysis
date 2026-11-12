using System;
using System.Collections.Generic;
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
        private const string MessageFormat = "Record property '{0}' in record type '{1}' should be init-only. CS-03.10.";
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

            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            // Built once per compilation, and only when a record has a property that could be reported.
            var interfaceSetterImplementations = new Lazy<ImmutableHashSet<IPropertySymbol>>(
                () => FindInterfaceSetterImplementations(startContext.Compilation, startContext.CancellationToken));

            startContext.RegisterSymbolAction(actionContext => AnalyzeSymbol(actionContext, interfaceSetterImplementations), SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, Lazy<ImmutableHashSet<IPropertySymbol>> interfaceSetterImplementations)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (!type.IsRecord)
            {
                return;
            }

            var properties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(HasSetAccessor)
                .Where(p => !p.IsStatic && !p.IsOverride && !p.IsIndexer && !p.SetMethod.IsReadOnly)
                .Where(p => HasExplicitSetAccessor(p, context.CancellationToken))
                .Where(p => !interfaceSetterImplementations.Value.Contains(p));

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
        /// Finds the record properties (and the properties they override) that implement an interface property with a 'set' accessor, in any record in the compilation.
        /// Replacing 'set' with 'init' on these would no longer satisfy the interface, including where a derived record implements the interface through an inherited property.
        /// </summary>
        private static ImmutableHashSet<IPropertySymbol> FindInterfaceSetterImplementations(Compilation compilation, CancellationToken cancellationToken)
        {
            var implementations = ImmutableHashSet.CreateBuilder<IPropertySymbol>(SymbolEqualityComparer.Default);

            var records = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type, cancellationToken)
                .OfType<INamedTypeSymbol>()
                .Where(t => t.IsRecord);

            foreach (var record in records)
            {
                implementations.UnionWith(GetInterfaceSetterImplementations(record));
            }

            return implementations.ToImmutable();
        }

        /// <summary>
        /// Gets the properties of the type that implement an interface property with a 'set' accessor, along with every property they override.
        /// </summary>
        internal static IEnumerable<IPropertySymbol> GetInterfaceSetterImplementations(INamedTypeSymbol type)
        {
            var interfaceSetters = type.AllInterfaces
                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
                .Where(HasSetAccessor);

            foreach (var interfaceSetter in interfaceSetters)
            {
                for (var implementation = type.FindImplementationForInterfaceMember(interfaceSetter) as IPropertySymbol;
                     implementation != null;
                     implementation = implementation.OverriddenProperty)
                {
                    yield return implementation.OriginalDefinition;
                }
            }
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
