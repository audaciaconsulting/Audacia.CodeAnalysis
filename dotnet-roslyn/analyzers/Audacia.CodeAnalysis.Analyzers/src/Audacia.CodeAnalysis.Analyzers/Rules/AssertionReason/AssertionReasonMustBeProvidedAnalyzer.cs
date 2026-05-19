using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AssertionReasonMustBeProvidedAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.AssertionReasonMustBeProvided;

        private const string Title = "Assertion has no reason provided";
        private const string MessageFormat = "Assertions must contain a reason";
        private const string Description = "Assertions must provide a reason for better clarity and maintainability.";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Maintainability, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            if (!nodeAnalysisContext.IsXunitTestMethod())
            {
                return;
            }

            var methodDeclaration = (MethodDeclarationSyntax)nodeAnalysisContext.Node;
            var body = (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody;

            if (body == null)
            {
                return;
            }

            var allInvocations = body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToList();

            foreach(var invocation in allInvocations)
            {
                var framework = invocation.GetAssertionFramework();

                if (framework == null)
                {
                    continue;
                }

                if (!framework.HasReasonArgument(invocation, nodeAnalysisContext.SemanticModel))
                {
                    var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                    nodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
