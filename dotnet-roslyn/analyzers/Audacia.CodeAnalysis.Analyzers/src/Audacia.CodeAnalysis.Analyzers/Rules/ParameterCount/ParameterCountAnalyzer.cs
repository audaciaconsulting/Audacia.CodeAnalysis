using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ParameterCount
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParameterCountAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Signature contains too many parameters";

        private const string ParameterCountMessageFormat =
            "{0} contains {1} parameters, which exceeds the maximum of {2} parameters.";

        private const string Description = "Don't declare signatures with more than a predefined number of parameters.";

        private static readonly DiagnosticDescriptor ParameterCountRule = new DiagnosticDescriptor(
            Id,
            Title,
            ParameterCountMessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            true,
            Description);

        private static readonly Action<CompilationStartAnalysisContext> RegisterCompilationStartAction =
            RegisterCompilationStart;

        private static readonly Action<SymbolAnalysisContext, EditorConfigSettingsReader> AnalyzeMethodAction =
            (context, settingsReader) => context.SkipEmptyName(_ => AnalyzeMethod(context, settingsReader));

        private static readonly SettingsKey MaxMethodParameterCountKey =
            new SettingsKey(Id, "max_method_parameter_count");

        private static readonly SettingsKey MaxConstructorParameterCountKey =
            new SettingsKey(Id, "max_constructor_parameter_count");

        private static IEnumerable<string> ExcludedParameterTypes = new List<string>
        {
            nameof(CancellationToken)
        };

        public const string Id = DiagnosticId.ParameterCount;

        public const int DefaultMaxParameterCount = 4;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(ParameterCountRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(RegisterCompilationStartAction);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var settingsReader = new EditorConfigSettingsReader(startContext.Options);
            startContext.RegisterSymbolAction(actionContext => AnalyzeMethodAction(actionContext, settingsReader),
                SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, EditorConfigSettingsReader settingsReader)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (!method.IsPropertyOrEventAccessor() && MemberRequiresAnalysis(method, context.CancellationToken))
            {
                var memberName = GetMemberName(method);
                var isConstructor = IsConstructor(method);

                ParameterSettings settings =
                    GetParameterSettings(method, settingsReader,
                        context.Symbol.DeclaringSyntaxReferences[0].SyntaxTree);

                var parameterCountInfo = new ParameterCountInfo(method, settings, isConstructor);

                AnalyzeParameters(context, parameterCountInfo, memberName);
            }
        }

        private static ParameterSettings GetParameterSettings(ISymbol symbol, EditorConfigSettingsReader settingsReader,
            SyntaxTree syntaxTree)
        {
            var maxParameterCount = DefaultMaxParameterCount;
            var maxConstructorParameterCount = DefaultMaxParameterCount;

            var attributes = symbol.GetAttributes();
            var maxCountAttribute =
                attributes.FirstOrDefault(att => att.AttributeClass.Name == "MaxParameterCountAttribute");
            if (maxCountAttribute != null)
            {
                var maxLengthArgument = maxCountAttribute.ConstructorArguments.First();
                var value = (int)maxLengthArgument.Value;
                maxParameterCount = value;
                maxConstructorParameterCount = value;
            }
            else
            {
                maxParameterCount = settingsReader.TryGetInt(syntaxTree, MaxMethodParameterCountKey) ??
                                    maxParameterCount;
                maxConstructorParameterCount = settingsReader.TryGetInt(syntaxTree, MaxConstructorParameterCountKey) ??
                                               maxConstructorParameterCount;
            }

            return new ParameterSettings(maxParameterCount, maxConstructorParameterCount);
        }

        private static bool MemberRequiresAnalysis(ISymbol member, CancellationToken cancellationToken)
        {
            return !member.IsExtern &&
                   !member.IsOverride &&
                   !member.HidesBaseMember(cancellationToken) &&
                   !member.IsRecordImplementation(cancellationToken) &&
                   !member.IsInterfaceImplementation();
        }

        private static string GetMemberName(IMethodSymbol method)
        {
            return IsConstructor(method) ? GetNameForConstructor(method) : GetNameForMethod(method);
        }

        private static bool IsConstructor(IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor;
        }

        private static string GetNameForConstructor(IMethodSymbol method)
        {
            var builder = new StringBuilder();
            builder.Append("Constructor for '");
            builder.Append(method.ContainingType.Name);
            builder.Append("'");

            return builder.ToString();
        }

        private static string GetNameForMethod(IMethodSymbol method)
        {
            var builder = new StringBuilder();

            builder.Append(method.GetKind());
            builder.Append(" '");
            builder.Append(method.Name);
            builder.Append("'");

            return builder.ToString();
        }

        private static void AnalyzeParameters(SymbolAnalysisContext context, ParameterCountInfo parameterCountInfo,
            string memberName)
        {
            var methodParameters = parameterCountInfo.MethodSymbol.Parameters;

            var parametersWithoutExcludedTypes = methodParameters.AsEnumerable()
                .Where(parameter =>
                {
                    var parameterTypeName = parameter.Type.Name;

                    return !ExcludedParameterTypes.Select(excludedParam => excludedParam)
                               .Contains(parameterTypeName);
                })
                .ToArray();

            if (parametersWithoutExcludedTypes.Length > parameterCountInfo.MaxParameterCount)
            {
                ReportParameterCount(context, parameterCountInfo, memberName, parametersWithoutExcludedTypes.Length);
            }
        }

        private static void ReportParameterCount(SymbolAnalysisContext context, ParameterCountInfo parameterCountInfo,
            string name, int parameterCount)
        {
            if (!parameterCountInfo.MethodSymbol.IsSynthesized())
            {
                var diagnostic = Diagnostic.Create(ParameterCountRule, parameterCountInfo.MethodSymbol.Locations[0],
                    name, parameterCount, parameterCountInfo.MaxParameterCount);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private class ParameterSettings
        {
            public int MaxParameterCount { get; }

            public int MaxConstructorParameterCount { get; }

            public ParameterSettings(int maxParameterCount, int maxConstructorParameterCount)
            {
                MaxParameterCount = maxParameterCount;
                MaxConstructorParameterCount = maxConstructorParameterCount;
            }
        }

        private class ParameterCountInfo
        {
            private readonly ParameterSettings _settings;

            private readonly bool _isConstructor;

            public IMethodSymbol MethodSymbol { get; }

            public int MaxParameterCount =>
                _isConstructor ? _settings.MaxConstructorParameterCount : _settings.MaxParameterCount;

            public ParameterCountInfo(IMethodSymbol methodSymbol, ParameterSettings settings,
                bool isConstructor = false)
            {
                MethodSymbol = methodSymbol;
                _settings = settings;
                _isConstructor = isConstructor;
            }
        }
    }
}
