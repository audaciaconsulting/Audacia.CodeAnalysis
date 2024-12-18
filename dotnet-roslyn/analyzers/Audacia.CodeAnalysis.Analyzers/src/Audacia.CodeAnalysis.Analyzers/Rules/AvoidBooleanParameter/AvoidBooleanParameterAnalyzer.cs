using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AvoidBooleanParameter
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidBooleanParameterAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Parameter in public or internal member is of type bool or bool?";
        private const string MessageFormat = "Parameter '{0}' is of type '{1}'";
        private const string Description = "Avoid signatures that take a bool parameter.";
        private const string Category = DiagnosticCategory.Maintainability;

        public const string Id = DiagnosticId.AvoidBooleanParameters;
        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        }

        private static void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            // Skip synthesized and deconstructor parameters
            if (parameter.ContainingSymbol.IsDeconstructor() || parameter.IsSynthesized())
            {
                return;
            }

            // Skip parameters that are part of a record's primary (positional) constructor
            if (IsPositionalRecordParameter(parameter, context))
            {
                return;
            }

            if (IsParameterAccessible(parameter) && parameter.Type.IsBooleanOrNullableBoolean())
            {
                AnalyzeBooleanParameter(parameter, context);
            }
        }

        private static bool IsParameterAccessible(IParameterSymbol parameter)
        {
            var containingMember = parameter.ContainingSymbol;
            return containingMember.DeclaredAccessibility != Accessibility.Private && containingMember.IsSymbolAccessibleFromRoot();
        }

        private static void AnalyzeBooleanParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            if (!containingMember.IsOverride
                && !containingMember.HidesBaseMember(context.CancellationToken)
                && !parameter.IsInterfaceImplementation()
                && !IsDisposablePattern(parameter))
            {
                var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.Type);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsDisposablePattern(IParameterSymbol parameter)
        {
            if (parameter != null && parameter.Name == "disposing")
            {
                var containingMethod = parameter.ContainingSymbol as IMethodSymbol;
                if (containingMethod != null && containingMethod.Name == "Dispose"
                    && containingMethod.IsVirtual
                    && containingMethod.DeclaredAccessibility == Accessibility.Protected)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPositionalRecordParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            var methodSymbol = parameter.ContainingSymbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            if (methodSymbol.MethodKind == MethodKind.Constructor &&
                methodSymbol.ContainingType.IsRecord &&
                methodSymbol.DeclaringSyntaxReferences.Length > 0)
            {
                var syntaxNode = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as RecordDeclarationSyntax;
                if (syntaxNode != null && syntaxNode.ParameterList != null)
                {
                    return syntaxNode.ParameterList.Parameters.Any(p => p.Identifier.Text == parameter.Name);
                }
            }

            return false;
        }
    }
}
