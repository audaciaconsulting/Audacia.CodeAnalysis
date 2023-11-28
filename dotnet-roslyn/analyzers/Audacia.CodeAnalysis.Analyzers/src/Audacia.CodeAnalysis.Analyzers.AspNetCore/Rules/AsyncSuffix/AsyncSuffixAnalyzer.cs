using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.AspNetCore.Rules.AsyncSuffix
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncSuffixAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.AsyncSuffix;

        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private const string MessageFormat = "Asynchronous method name '{0}' is not suffixed with 'Async'.";

        private const string Title = "Asynchronous method name is not suffixed with 'Async'.";

        private const string Description = "Asynchronous method names should be suffixed with 'Async'.";

        private const string Category = DiagnosticCategory.Naming;

        private const bool IsEnabled = true;

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, Category, Severity, IsEnabled, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        /// <summary>
        /// A collection of syntax kinds that we want our analyzer to read.
        /// </summary>
        private readonly SyntaxKind[] _syntaxKinds =
        {
            SyntaxKind.MethodDeclaration
        };

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(
                analysisContext =>
                {
                    analysisContext.RegisterSyntaxNodeAction(
                        AnalyzeMethodDeclaration,
                        _syntaxKinds
                    );
                }
            );
        }

        /// <summary>
        /// The method declaration analysis includes the following checks:
        /// 1. determines whether the method is asynchronous
        /// 2. determines whether the method is awaitable
        /// 3. determines whether the method represents a controller action
        ///
        /// A diagnostic will be reported if a method is asynchronous but the method name has not been suffixed with 'Async'.
        /// Please note, this DOES NOT apply to any methods that represent a controller action.
        /// </summary>
        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var isAsynchronous = nodeAnalysisContext.IsAsynchronous();

            var isAwaitable = nodeAnalysisContext.IsAwaitable();

            if (isAsynchronous || isAwaitable)
            {
                var isAsyncSuffixed = nodeAnalysisContext.IsAsyncSuffixed();

                var isControllerAction = nodeAnalysisContext.IsControllerAction();

                if (!isAsyncSuffixed && !isControllerAction)
                {
                    var location = nodeAnalysisContext.Node.GetLocation();

                    var methodName = nodeAnalysisContext.GetMethodName();

                    var diagnostic = Diagnostic.Create(Rule, location, methodName);

                    nodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
