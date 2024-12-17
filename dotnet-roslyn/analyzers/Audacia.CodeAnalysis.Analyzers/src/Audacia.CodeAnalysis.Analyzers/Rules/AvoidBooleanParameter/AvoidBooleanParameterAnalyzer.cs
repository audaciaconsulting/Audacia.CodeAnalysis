using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AvoidBooleanParameter
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidBooleanParameterAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Parameter in public or internal member is of type bool or bool?";
        private const string MessageFormat = "Parameter '{0}' is of type '{1}'";
        private const string Description = "Avoid signatures that take a bool parameter.";

        private static string Category = DiagnosticCategory.Maintainability;
        public const string Id = DiagnosticId.AvoidBooleanParameters;
        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Id, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true,
            Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // Register an action that runs on each parameter syntax node.
            context.SafeRegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }

        private static void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            // Determine if this parameter comes from a record’s positional parameter list
            var isRecordParam = false;
            foreach (var reference in context.Symbol.DeclaringSyntaxReferences)
            {
                var referenceSyntax = reference.GetSyntax(context.CancellationToken);
                // Check if the parameter syntax is inside a RecordDeclaration node.
                // If it is from the primary constructor (positional record), the immediate parent will be a ParameterList, 
                // and above that a RecordDeclaration. We check `referenceSyntax.Parent.Parent`:
                isRecordParam = referenceSyntax.Parent?.Parent?.IsKind(SyntaxKind.RecordDeclaration) == true;
            }

            // We skip analysis if:
            // 1. The containing symbol is a deconstructor (e.g., Deconstruct method on a record)
            // 2. The parameter is synthesized by the compiler
            // 3. The parameter belongs to a positional record (as per the new requirement)
            if (parameter.ContainingSymbol.IsDeconstructor() || parameter.IsSynthesized() || isRecordParam)
            {
                return;
            }

            // Check if the parameter is accessible and of bool or bool? type.
            if (IsParameterAccessible(parameter) && parameter.Type.IsBooleanOrNullableBoolean())
            {
                AnalyzeBooleanParameter(parameter, context);
            }
        }

        private static bool IsParameterAccessible(IParameterSymbol parameter)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            // Check if the containing member is non-private and accessible from the root.
            return containingMember.DeclaredAccessibility != Accessibility.Private &&
                   containingMember.IsSymbolAccessibleFromRoot();
        }

        private static void AnalyzeBooleanParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            // We do not raise a diagnostic if the parameter is an override, hides a base member, implements an interface method, 
            // or is part of a known disposable pattern (Dispose method with a bool named Disposing).
            if (!containingMember.IsOverride &&
                !containingMember.HidesBaseMember(context.CancellationToken) &&
                !parameter.IsInterfaceImplementation() &&
                !IsDisposablePattern(parameter))
            {
                var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.Type);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsDisposablePattern(IParameterSymbol parameter)
        {
            // Special case: a Dispose(bool Disposing) pattern commonly found in certain classes
            try
            {
                var containingSymbol = (IMethodSymbol)parameter.ContainingSymbol;
                if (parameter.Name == "Disposing" && containingSymbol.Name == "Dispose")
                {
                    if (containingSymbol.IsVirtual && containingSymbol.DeclaredAccessibility == Accessibility.Protected)
                    {
                        return true;
                    }
                }
            }
            catch (InvalidCastException)
            {
                // If we cannot cast to IMethodSymbol, just return false.
            }
            return false;
        }
    }

}
