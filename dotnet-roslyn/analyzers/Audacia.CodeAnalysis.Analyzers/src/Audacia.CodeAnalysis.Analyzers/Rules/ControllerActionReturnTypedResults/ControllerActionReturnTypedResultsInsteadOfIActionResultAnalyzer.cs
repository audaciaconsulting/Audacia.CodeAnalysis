using System.Collections.Immutable;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionReturnTypedResults
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControllerActionReturnTypedResultsInsteadOfIActionResultAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.UseTypedResultsInsteadOfIActionResult;

        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private const string MessageFormat = "Controller action name '{0}' should return a TypedResult rather than an IActionResult";

        private const string Title = "Controller action should return a TypedResult rather than an IActionResult";

        private const string Description = "Controller actions should return a TypedResult rather than an IActionResult.";

        private const string Category = DiagnosticCategory.Maintainability;

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
        /// 1. determines whether the method is a controller
        /// 2. determines whether the controller has return type TypedResults instead of IActionResult
        ///
        /// A diagnostic will be reported if a method is a controller but the method has return type IActionResult instead of TypedResults.
        /// Please note, this ONLY applies to controller actions.
        /// </summary>
        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var isController = nodeAnalysisContext.IsControllerAction();

            if (isController)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

                var nameSpaces = nodeAnalysisContext.SemanticModel.GetTypeInfo(methodDeclarationSyntax.ReturnType).Type.ContainingNamespace;

                var returnType = methodDeclarationSyntax.ReturnType;

                var returnTypeSymbol = nodeAnalysisContext.SemanticModel.GetSymbolInfo(returnType).Symbol as INamedTypeSymbol;

                if (returnTypeSymbol != null && returnTypeSymbol.IsGenericType) 
                {
                    var genericTypeArgument = returnTypeSymbol.TypeArguments.FirstOrDefault();

                    nameSpaces = genericTypeArgument.ContainingNamespace;
                }

                if (returnType.ToString().Contains("IActionResult") && nameSpaces.ToDisplayString().Contains("Microsoft.AspNetCore.Mvc"))
                {
                    var location = returnType.GetLocation();

                    var methodName = nodeAnalysisContext.GetMethodName();

                    var diagnostic = Diagnostic.Create(Rule, location, methodName);

                    nodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
