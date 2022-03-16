using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.OverloadShouldCallOtherOverload
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OverloadShouldCallOtherOverloadAnalyzer : DiagnosticAnalyzer
    {
        private struct OverloadsInfo
        {
            public IReadOnlyCollection<IMethodSymbol> MethodGroup { get; }

            public IMethodSymbol LongestOverload { get; }

            public SymbolAnalysisContext Context { get; }

            public OverloadsInfo(IReadOnlyCollection<IMethodSymbol> methodGroup,
                IMethodSymbol longestOverload, SymbolAnalysisContext context)
            {
                MethodGroup = methodGroup;
                LongestOverload = longestOverload;
                Context = context;
            }
        }

        public const string Id = DiagnosticId.OverloadShouldCallOtherOverload;
        private const string Title = "Method overload should call another overload";

        private const string MessageFormat =
            "Parameter order in '{0}' does not match with the parameter order of the longest overload.";

        private const string InvokeMessageFormat = "Overloaded method '{0}' should call another overload.";

        private const string MakeVirtualMessageFormat = "Method overload with the most parameters should be virtual.";

        private const string OrderMessageFormat =
            "Parameter order in '{0}' does not match with the parameter order of the longest overload.";

        private const string Description = "Call the more overloaded method from other overloads.";
        private const string Category = DiagnosticCategory.Maintainability;
        private const DiagnosticSeverity Serverity = DiagnosticSeverity.Warning;
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            Serverity,
            true,
            Description);
        private static readonly Action<CompilationStartAnalysisContext> RegisterCompilationStartAction = RegisterCompilationStart;

        private static readonly DiagnosticDescriptor MakeVirtualRule = new DiagnosticDescriptor(Id, Title,
            MakeVirtualMessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        private static readonly DiagnosticDescriptor OrderRule = new DiagnosticDescriptor(Id, Title, OrderMessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description);

        private static readonly DiagnosticDescriptor InvokeRule = new DiagnosticDescriptor(Id, Title,
            InvokeMessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly ImmutableArray<MethodKind> RegularMethodKinds = new[]
        {
            MethodKind.Ordinary,
            MethodKind.ExplicitInterfaceImplementation,
            MethodKind.ReducedExtension
        }.ToImmutableArray();

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(RegisterCompilationStartAction);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var settingsReader = new EditorConfigSettingsReader(startContext.Options);
            startContext.RegisterSymbolAction(actionContext => AnalyzeNamedType(actionContext), SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var controllerBaseTypeNames
                = new List<string>
                {
                    "Controller",
                    "ControllerBase"
                };

            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            {
                return;
            }

            if (controllerBaseTypeNames.Contains(type.BaseType.Name.ToString()))
            {
                return;
            }

            IGrouping<string, IMethodSymbol>[] methodGroups = GetRegularMethodsInTypeHierarchy(type, context.CancellationToken)
                .GroupBy(method => method.Name).Where(HasAtLeastTwoItems).ToArray();

            foreach (IGrouping<string, IMethodSymbol> methodGroup in methodGroups)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                AnalyzeMethodGroup(methodGroup.ToArray(), type, context);
            }
        }

        private static IEnumerable<IMethodSymbol> GetRegularMethodsInTypeHierarchy(INamedTypeSymbol type,
           CancellationToken cancellationToken)
        {
            return EnumerateSelfWithBaseTypes(type).SelectMany(nextType => GetRegularMethodsInType(nextType, cancellationToken))
                .ToArray();
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateSelfWithBaseTypes(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol nextType = type; nextType != null; nextType = nextType.BaseType)
            {
                yield return nextType;
            }
        }

        private static IEnumerable<IMethodSymbol> GetRegularMethodsInType(INamedTypeSymbol type,
           CancellationToken cancellationToken)
        {
            return type.GetMembers().OfType<IMethodSymbol>().Where(method => IsRegularMethod(method, cancellationToken))
                .ToArray();
        }

        private static bool IsRegularMethod(IMethodSymbol method, CancellationToken cancellationToken)
        {
            var test = method.GetAttributes();
            return RegularMethodKinds.Contains(method.MethodKind) && !method.IsSynthesized() &&
                HasMethodBody(method, cancellationToken) && !method.IsControllerAction();
        }

        private static bool HasMethodBody(IMethodSymbol method, CancellationToken cancellationToken)
        {
            return method.TryGetBodySyntaxForMethod(cancellationToken) != null;
        }

        private static bool HasAtLeastTwoItems<T>(IEnumerable<T> source)
        {
            return source.Skip(1).Any();
        }

        private static void AnalyzeMethodGroup(IReadOnlyCollection<IMethodSymbol> methodGroup,
            INamedTypeSymbol activeType, SymbolAnalysisContext context)
        {
            methodGroup.FirstOrDefault();

            IMethodSymbol longestOverload = TryGetSingleLongestOverload(methodGroup);

            if (longestOverload != null)
            {
                if (longestOverload.ContainingType.IsEqualTo(activeType) && CanBeMadeVirtual(longestOverload))
                {
                    IMethodSymbol methodToReport = longestOverload.PartialImplementationPart ?? longestOverload;
                    context.ReportDiagnostic(Diagnostic.Create(MakeVirtualRule, methodToReport.Locations[0]));
                }

                var info = new OverloadsInfo(methodGroup, longestOverload, context);

                AnalyzeOverloads(info, activeType);
            }
        }

        private static void AnalyzeOverloads(OverloadsInfo info, INamedTypeSymbol activeType)
        {
            IEnumerable<IMethodSymbol> overloadsInActiveType = info.MethodGroup.Where(method =>
                !method.IsEqualTo(info.LongestOverload) && method.ContainingType.IsEqualTo(activeType));

            foreach (IMethodSymbol overload in overloadsInActiveType)
            {
                AnalyzeOverload(info, overload);
            }
        }

        private static void AnalyzeOverload(OverloadsInfo info, IMethodSymbol overload)
        {
            if (!overload.IsOverride && !overload.IsInterfaceImplementation() &&
                !overload.HidesBaseMember(info.Context.CancellationToken))
            {
                CompareOrderOfParameters(overload, info.LongestOverload, info.Context);
            }

            var invocationWalker = new MethodInvocationWalker(info.MethodGroup);

            if (!InvokesAnotherOverload(overload, invocationWalker, info.Context))
            {
                IMethodSymbol methodToReport = overload.PartialImplementationPart ?? overload;

                info.Context.ReportDiagnostic(Diagnostic.Create(InvokeRule, methodToReport.Locations[0],
                    methodToReport.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
        }

        private static bool InvokesAnotherOverload(IMethodSymbol methodToAnalyze,
            MethodInvocationWalker invocationWalker, SymbolAnalysisContext context)
        {
            IOperation operation = methodToAnalyze.TryGetOperationBlockForMethod(context.Compilation, context.CancellationToken);

            if (operation != null)
            {
                invocationWalker.AnalyzeBlock(operation, methodToAnalyze);
                return invocationWalker.HasFoundInvocation;
            }

            return false;
        }

        private static IMethodSymbol TryGetSingleLongestOverload(IReadOnlyCollection<IMethodSymbol> methodGroup)
        {
            IGrouping<int, IMethodSymbol> overloadsWithHighestParameterCount =
                methodGroup.GroupBy(group => group.Parameters.Length).OrderByDescending(group => group.Key).First();

            return overloadsWithHighestParameterCount.Skip(1).FirstOrDefault() == null
                ? overloadsWithHighestParameterCount.First()
                : null;
        }

        private static bool CanBeMadeVirtual(IMethodSymbol method)
        {
            return !method.IsStatic && method.DeclaredAccessibility != Accessibility.Private && !method.ContainingType.IsSealed &&
                method.ContainingType.TypeKind != TypeKind.Struct && !method.IsVirtual && !method.IsOverride &&
                !method.ExplicitInterfaceImplementations.Any();
        }

        private static void CompareOrderOfParameters(IMethodSymbol method, IMethodSymbol longestOverload,
            SymbolAnalysisContext context)
        {
            List<IParameterSymbol> parametersInLongestOverload = longestOverload.Parameters.ToList();

            if (!AreParametersDeclaredInSameOrder(method, parametersInLongestOverload))
            {
                context.ReportDiagnostic(Diagnostic.Create(OrderRule, method.Locations[0],
                    method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
        }

        private static bool AreParametersDeclaredInSameOrder(IMethodSymbol method,
            List<IParameterSymbol> parametersInLongestOverload)
        {
            return AreRegularParametersDeclaredInSameOrder(method, parametersInLongestOverload) &&
                AreDefaultParametersDeclaredInSameOrder(method, parametersInLongestOverload);
        }

        private static bool AreRegularParametersDeclaredInSameOrder(IMethodSymbol method,
            List<IParameterSymbol> parametersInLongestOverload)
        {
            List<IParameterSymbol> regularParametersInMethod = method.Parameters.Where(IsRegularParameter).ToList();

            List<IParameterSymbol> regularParametersInLongestOverload =
                parametersInLongestOverload.Where(IsRegularParameter).ToList();

            return AreParametersDeclaredInSameOrder(regularParametersInMethod, regularParametersInLongestOverload);
        }

        private static bool IsRegularParameter(IParameterSymbol parameter)
        {
            return !parameter.HasExplicitDefaultValue && !parameter.IsParams;
        }

        private static bool AreDefaultParametersDeclaredInSameOrder(IMethodSymbol method,
            List<IParameterSymbol> parametersInLongestOverload)
        {
            List<IParameterSymbol> defaultParametersInMethod = method.Parameters.Where(IsParameterWithDefaultValue).ToList();

            List<IParameterSymbol> defaultParametersInLongestOverload =
                parametersInLongestOverload.Where(IsParameterWithDefaultValue).ToList();

            return AreParametersDeclaredInSameOrder(defaultParametersInMethod, defaultParametersInLongestOverload);
        }

        private static bool IsParameterWithDefaultValue(IParameterSymbol parameter)
        {
            return parameter.HasExplicitDefaultValue && !parameter.IsParams;
        }

        private static bool AreParametersDeclaredInSameOrder(IList<IParameterSymbol> parameters,
            List<IParameterSymbol> parametersInLongestOverload)
        {
            for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
            {
                string parameterName = parameters[parameterIndex].Name;

                int indexInLongestOverload = parametersInLongestOverload.FindIndex(parameter => parameter.Name == parameterName);

                if (indexInLongestOverload != -1 && indexInLongestOverload != parameterIndex)
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class MethodInvocationWalker : ExplicitOperationWalker
        {
            private readonly IReadOnlyCollection<IMethodSymbol> methodGroup;

            private IMethodSymbol containingMethod;

            public bool HasFoundInvocation { get; private set; }

            public MethodInvocationWalker(IReadOnlyCollection<IMethodSymbol> methodGroup)
            {
                this.methodGroup = methodGroup;
            }

            public void AnalyzeBlock(IOperation block, IMethodSymbol method)
            {
                containingMethod = method;
                HasFoundInvocation = false;

                Visit(block);
            }

            public override void VisitInvocation(IInvocationOperation operation)
            {
                if (HasFoundInvocation)
                {
                    return;
                }

                foreach (IMethodSymbol methodToFind in methodGroup)
                {
                    if (!methodToFind.IsEqualTo(containingMethod))
                    {
                        VerifyInvocation(operation, methodToFind);
                    }
                }

                if (!HasFoundInvocation)
                {
                    base.VisitInvocation(operation);
                }
            }

            private void VerifyInvocation(IInvocationOperation operation, IMethodSymbol methodToFind)
            {
                if (methodToFind.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    ScanExplicitInterfaceInvocation(operation, methodToFind);
                }
                else
                {
                    if (methodToFind.OriginalDefinition.IsEqualTo(operation.TargetMethod.OriginalDefinition))
                    {
                        HasFoundInvocation = true;
                    }
                }
            }

            private void ScanExplicitInterfaceInvocation(IInvocationOperation operation,
                IMethodSymbol methodToFind)
            {
                foreach (IMethodSymbol interfaceMethod in methodToFind.ExplicitInterfaceImplementations)
                {
                    if (operation.TargetMethod.OriginalDefinition.IsEqualTo(interfaceMethod.OriginalDefinition))
                    {
                        HasFoundInvocation = true;
                        break;
                    }
                }
            }
        }

    }
}
