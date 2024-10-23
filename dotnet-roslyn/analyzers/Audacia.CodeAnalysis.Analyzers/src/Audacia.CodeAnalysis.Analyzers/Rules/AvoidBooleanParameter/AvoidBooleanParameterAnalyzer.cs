using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        private static readonly Action<SyntaxNodeAnalysisContext> AnalyzeParameterAction = context => AnalysisContextExtensions.SkipEmptyName(context, AnalyzeParameter);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.SafeRegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }

        private static void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;
            var isRecordParam = false; 
            foreach (var references in context.Symbol.DeclaringSyntaxReferences)
            {
                var reference = references.GetSyntax(context.CancellationToken);
                isRecordParam = reference.Parent.Parent.IsKind(SyntaxKind.RecordDeclaration);
            }
      
            if (parameter.ContainingSymbol.IsDeconstructor() || parameter.IsSynthesized() || isRecordParam)
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
            ISymbol containingMember = parameter.ContainingSymbol;

            return containingMember.DeclaredAccessibility != Accessibility.Private && containingMember.IsSymbolAccessibleFromRoot();
        }

        private static void AnalyzeBooleanParameter(IParameterSymbol parameter, SymbolAnalysisContext context)
        {
            ISymbol containingMember = parameter.ContainingSymbol;

            if (!containingMember.IsOverride && !containingMember.HidesBaseMember(context.CancellationToken) && !parameter.IsInterfaceImplementation() &&
                !IsDisposablePattern(parameter))
            {
                var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.Type);
                context.ReportDiagnostic(diagnostic);
            }
        }
        
        private static bool IsDisposablePattern(IParameterSymbol parameter)
        {
            try
            {
                var containingSymbol = (IMethodSymbol)parameter.ContainingSymbol;
                
                if (parameter.Name == "Disposing" && (containingSymbol.Name == "Dispose"))
                {
                    if (containingSymbol.IsVirtual && containingSymbol.DeclaredAccessibility == Accessibility.Protected)
                    {
                        return true;
                    }
                }
            }
            catch (InvalidCastException)
            {
                //We cant cast this so its false
                return false;
            }

            return false;
        }
    }
}
