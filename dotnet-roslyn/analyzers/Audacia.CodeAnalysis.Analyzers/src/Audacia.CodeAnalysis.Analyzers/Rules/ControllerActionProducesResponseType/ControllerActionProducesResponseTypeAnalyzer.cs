using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionProducesResponseType
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControllerActionProducesResponseTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.ControllerActionProducesResponseType;

        public const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private const string MessageFormat = "Controller action name '{0}' has no [ProducesResponseType] attribute.";

        private const string Title = "Controller action has no [ProducesResponseType] attribute.";

        private const string Description = "Controller actions should have at least one [ProducesResponseType] attribute.";

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
        /// 2. determines whether the controller has at least one [ProducesResponseType] attribute
        ///
        /// A diagnostic will be reported if a method is a controller but the method does not have a [ProducesResponseType] attribute.
        /// Please note, this ONLY applies to controller actions.
        /// </summary>
        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var isController = nodeAnalysisContext.IsControllerAction();
            
            if (isController)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

                var methodAttributes = methodDeclarationSyntax.GetMethodAttributes();

                var hasProducesResponseType = methodAttributes
                .Any(
                    name =>
                        name.Equals("ProducesResponseType")
                );

                if (!hasProducesResponseType)
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
