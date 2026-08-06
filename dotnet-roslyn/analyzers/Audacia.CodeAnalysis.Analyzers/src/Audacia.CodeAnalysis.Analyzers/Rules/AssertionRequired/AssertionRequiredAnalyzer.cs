using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AssertionRequiredAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ISet<string> MoqVerificationMethodNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Verify",
            "VerifyAll",
            "VerifyGet",
            "VerifySet",
            "VerifyAdd",
            "VerifyRemove",
            "VerifyNoOtherCalls",
        };

        private static readonly ISet<string> NSubstituteVerificationMethodNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Received",
            "ReceivedWithAnyArgs",
            "DidNotReceive",
            "DidNotReceiveWithAnyArgs",
            "InOrder",
        };

        public const string Id = DiagnosticId.AssertionRequired;

        private const string Title = "Test method name '{0}' has no assertions";
        private const string MessageFormat = "Test methods must contain at least one assertion";
        private const string Description = "When a test method has no assertions, it should be updated to include at least one assertion.";

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
            var isTestMethod = nodeAnalysisContext.IsXunitTestMethod();
            if (!isTestMethod)
            {
                return;
            }
            
            var methodDeclaration = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var containsAssertion = HasAssertion(
                methodDeclaration,
                nodeAnalysisContext.SemanticModel,
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));

            if (!containsAssertion)
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        private static bool HasAssertion(
            MethodDeclarationSyntax methodDeclaration,
            SemanticModel semanticModel,
            HashSet<IMethodSymbol> visitedMethods)
        {
            SyntaxNode body = (SyntaxNode)methodDeclaration.Body ?? methodDeclaration.ExpressionBody;

            if(body == null)
            {
                return false;
            }

            var allInvocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            foreach(var invocation in allInvocations)
            {
                var framework = invocation.GetAssertionFramework();

                if (framework != null)
                {
                    return true;
                }

                if (IsMockVerificationCall(invocation, semanticModel))
                {
                    return true;
                }

                // Not a known assertion call — check whether it is a helper method call that contains assertions.
                var (helperMethod, helperSemanticModel) = invocation.ResolveHelperMethodDeclarationWithSemanticModel(semanticModel);
                if (helperMethod == null)
                {
                    continue;
                }

                var methodSymbol = helperSemanticModel.GetDeclaredSymbol(helperMethod);
                if (methodSymbol == null || !visitedMethods.Add(methodSymbol))
                {
                    continue;
                }

                if (HasAssertion(helperMethod, helperSemanticModel, visitedMethods))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMockVerificationCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            var methodSymbol = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            return IsMoqVerificationCall(methodSymbol) || IsNSubstituteVerificationCall(methodSymbol);
        }

        private static bool IsMoqVerificationCall(IMethodSymbol methodSymbol)
        {
            var containingNamespace = methodSymbol.ContainingNamespace?.ToDisplayString();
            if (containingNamespace == null)
            {
                return false;
            }

            return (string.Equals(containingNamespace, "Moq", StringComparison.Ordinal) ||
                    containingNamespace.StartsWith("Moq.", StringComparison.Ordinal)) &&
                   MoqVerificationMethodNames.Contains(methodSymbol.Name);
        }

        private static bool IsNSubstituteVerificationCall(IMethodSymbol methodSymbol)
        {
            var containingNamespace = methodSymbol.ContainingNamespace?.ToDisplayString();
            if (containingNamespace == null)
            {
                return false;
            }

            return (string.Equals(containingNamespace, "NSubstitute", StringComparison.Ordinal) ||
                    containingNamespace.StartsWith("NSubstitute.", StringComparison.Ordinal)) &&
                   NSubstituteVerificationMethodNames.Contains(methodSymbol.Name);
        }
    }
}
