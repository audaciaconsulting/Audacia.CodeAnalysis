using System;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionReturnTypedResults
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControllerActionReturnTypedResultsAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.DoNotUseProducesResponseTypeWithTypedResults;

        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private const string MessageFormat = "[ProducesResponseType] attribute should not be applied when using TypedResults";

        private const string Title = "Controller action has [ProducesResponseType] attribute when return type is TypedResult";

        private const string Description = "Controller actions should not use [ProducesResponseType] attribute when return type is TypedResult.";

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
        /// 2. determines whether the controller does not have [ProducesResponseType] attribute when return type is TypedResults
        ///
        /// A diagnostic will be reported if a method is a controller but the method has a [ProducesResponseType] attribute with TypesResults as return type.
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

                var methodAttributes = methodDeclarationSyntax.GetMethodAttributes();

                var attribute = methodDeclarationSyntax.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(a => a.Name.ToString() == "ProducesResponseType");

                if (attribute != null)
                {
                    var attributeNameSpace = nodeAnalysisContext.SemanticModel.GetTypeInfo(attribute).Type.ContainingNamespace;

                    if (attributeNameSpace.ToDisplayString().Contains("Microsoft.AspNetCore.Mvc") && 
                        returnType.ToString().Contains("Results") && 
                        nameSpaces.ToDisplayString().Contains("Microsoft.AspNetCore.Http"))
                    {
                        var location = attribute.GetLocation();

                        var diagnostic = Diagnostic.Create(Rule, location, attribute.Name);

                        nodeAnalysisContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
