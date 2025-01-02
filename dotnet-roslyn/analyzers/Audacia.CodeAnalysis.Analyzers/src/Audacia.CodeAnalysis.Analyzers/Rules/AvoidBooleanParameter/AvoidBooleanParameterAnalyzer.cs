﻿using Microsoft.CodeAnalysis.Diagnostics;
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

        /// <summary>
        /// Initializes the analyzer by registering actions to be executed during code analysis.
        /// </summary>
        /// <param name="context">The context in which the analyzer is running.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Configure the analyzer to ignore generated code, focusing only on user-written code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // Register an action to analyze each parameter symbol in the code.
            context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        }

        /// <summary>
        /// Analyzes a parameter symbol to determine if it violates the avoid boolean parameter rule.
        /// </summary>
        /// <param name="context">The context of the symbol being analyzed.</param>
        private static void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            // Skip parameters that are auto-generated by the compiler or part of deconstructors.
            if (ParameterIsAutoGenerated(parameter))
            {
                return;
            }

            // Skip parameters that are part of a record's primary (positional) constructor.
            if (IsPositionalRecordParameter(parameter, context))
            {
                return;
            }

            // Check if the parameter is accessible (public or internal) and of type bool or bool?.
            if (IsParameterAccessible(parameter) && parameter.Type.IsBooleanOrNullableBoolean())
            {
                AnalyzeBooleanParameter(parameter, context);
            }
        }

        /// <summary>
        /// Determines if the given parameter is auto-generated by the compiler.
        /// This helps in avoiding false positives by excluding such parameters from analysis.
        /// </summary>
        /// <param name="parameter">The parameter symbol to check.</param>
        /// <returns><c>True</c> if the parameter is auto-generated; otherwise, <c>false</c>.</returns>
        private static bool ParameterIsAutoGenerated(IParameterSymbol parameter)
        {
            // A parameter is considered auto-generated if it is part of a deconstructor or is synthesized by the compiler.
            return parameter.ContainingSymbol.IsDeconstructor() || parameter.IsSynthesized();
        }

        /// <summary>
        /// Determines if the parameter's containing member is accessible (public or internal).
        /// Private members are excluded from analysis to focus on APIs exposed to other components.
        /// </summary>
        /// <param name="parameter">The parameter symbol to check.</param>
        /// <returns><c>True</c> if the parameter is in an accessible member; otherwise, <c>false</c>.</returns>
        private static bool IsParameterAccessible(IParameterSymbol parameter)
        {
            var containingMember = parameter.ContainingSymbol;
            // The member must not be private and must be accessible from the root (e.g., accessible outside the assembly).
            return containingMember.DeclaredAccessibility != Accessibility.Private && containingMember.IsSymbolAccessibleFromRoot();
        }

        /// <summary>
        /// Analyzes a boolean parameter to determine if it should trigger a diagnostic.
        /// Excludes certain scenarios where reporting the diagnostic would be inappropriate.
        /// </summary>
        /// <param name="parameter">The boolean parameter symbol to analyze.</param>
        /// <param name="context">The context of the symbol being analyzed.</param>
        private static void AnalyzeBooleanParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            // Exclude parameters in overridden methods, methods hiding base members, interface implementations, and disposable patterns.
            if (!containingMember.IsOverride
                && !containingMember.HidesBaseMember(context.CancellationToken)
                && !parameter.IsInterfaceImplementation()
                && !IsDisposablePattern(parameter))
            {
                // Create a diagnostic at the location of the parameter.
                var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.Type);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if the parameter is part of the disposable pattern.
        /// Specifically, it checks for the common pattern of a <c>Dispose(bool disposing)</c> method.
        /// </summary>
        /// <param name="parameter">The parameter symbol to check.</param>
        /// <returns><c>True</c> if the parameter is part of the disposable pattern; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Determines if the parameter is part of a record's primary (positional) constructor.
        /// Positional parameters in records are treated as properties and are excluded from analysis.
        /// </summary>
        /// <param name="parameter">The parameter symbol to check.</param>
        /// <param name="context">The context of the symbol being analyzed.</param>
        /// <returns><c>True</c> if the parameter is part of a record's primary constructor; otherwise, <c>false</c>.</returns>
        private static bool IsPositionalRecordParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            var methodSymbol = parameter.ContainingSymbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            // Check if the method is a constructor of a record type.
            if (methodSymbol.MethodKind == MethodKind.Constructor &&
                methodSymbol.ContainingType.IsRecord &&
                methodSymbol.DeclaringSyntaxReferences.Length > 0)
            {
                // Retrieve the syntax node for the constructor.
                var syntaxNode = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as RecordDeclarationSyntax;
                if (syntaxNode != null && syntaxNode.ParameterList != null)
                {
                    // Check if the parameter name matches any of the record's primary constructor parameters.
                    return syntaxNode.ParameterList.Parameters.Any(p => p.Identifier.Text == parameter.Name);
                }
            }

            return false;
        }
    }
}
