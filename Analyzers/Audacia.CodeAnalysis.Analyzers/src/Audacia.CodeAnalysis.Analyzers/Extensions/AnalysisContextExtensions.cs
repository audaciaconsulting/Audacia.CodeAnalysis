using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    internal static class AnalysisContextExtensions
    {
        internal static void SkipEmptyName(this SymbolAnalysisContext context, Action<SymbolAnalysisContext> action)
        {
            if (!string.IsNullOrEmpty(context.Symbol.Name))
            {
                action(context);
            }
        }

        internal static void SkipEmptyName(this SyntaxNodeAnalysisContext context, Action<SymbolAnalysisContext> action)
        {
            SymbolAnalysisContext symbolContext = context.ToSymbolContext();
            symbolContext.SkipEmptyName(_ => action(symbolContext));
        }

        internal static SymbolAnalysisContext ToSymbolContext(this SyntaxNodeAnalysisContext syntaxContext)
        {
            ISymbol symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);

            return SyntaxToSymbolContext(syntaxContext, symbol);
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext syntaxContext, ISymbol symbol)
        {
            return new SymbolAnalysisContext(symbol, syntaxContext.SemanticModel.Compilation, syntaxContext.Options,
                syntaxContext.ReportDiagnostic, _ => true, syntaxContext.CancellationToken);
        }

        internal static void SkipInvalid(this OperationAnalysisContext context, Action<OperationAnalysisContext> action)
        {
            if (!context.Operation.HasErrors(context.Compilation, context.CancellationToken))
            {
                action(context);
            }
        }

        internal static void SkipInvalid(this OperationBlockAnalysisContext context, Action<OperationBlockAnalysisContext> action)
        {
            if (!context.OperationBlocks.Any(block => block.HasErrors(context.Compilation, context.CancellationToken)))
            {
                action(context);
            }
        }

        /// <summary>
        /// Returns true if a method is an asynchronous method.
        /// </summary>
        internal static bool IsAsynchronous(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodSymbol = nodeAnalysisContext.GetMethodSymbol();

            if (methodSymbol == null)
            {
                return false;
            }

            return methodSymbol.IsAsync;
        }

        /// <summary>
        /// Returns true if a method is an awaitable method.
        /// </summary>
        internal static bool IsAwaitable(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodReturnType = GetMethodSymbol(nodeAnalysisContext).ReturnType.MetadataName;

            var awaitableTypeNames
                = new List<string>
                {
                    typeof(Task).FullName,
                    typeof(Task<>).FullName,
                    typeof(ValueTask).FullName,
                    typeof(ValueTask<>).FullName,
                    typeof(IAsyncEnumerable<>).FullName
                };

            var awaitableTypes = awaitableTypeNames
                .Select(
                    awaitableTypeName =>
                        nodeAnalysisContext
                            .SemanticModel
                            .Compilation
                            .GetTypeByMetadataName(awaitableTypeName)
                            ?.MetadataName
                )
                .ToList();

            var isAwaitable = awaitableTypes
                .Any(
                    awaitableType => methodReturnType.Equals(awaitableType, StringComparison.CurrentCultureIgnoreCase)
                );

            return isAwaitable;
        }

        /// <summary>
        /// Returns true if method has any http action attributes (get/post/delete/put, etc)
        /// </summary>
        internal static bool IsControllerAction(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var methodAttributes = GetMethodAttributes(methodDeclarationSyntax);

            var controllerActionAttributeNames
                = new List<string>
                {
                    "HttpDelete",
                    "HttpGet",
                    "HttpHead",
                    "HttpOptions",
                    "HttpPatch",
                    "HttpPost",
                    "HttpPut"
                };

            var isControllerAction = methodAttributes
                .Any(
                    name =>
                        controllerActionAttributeNames.Contains(name, StringComparer.InvariantCultureIgnoreCase)
                );

            var isControllerBaseType = IsControllerBaseType(nodeAnalysisContext);

            return isControllerAction || isControllerBaseType;
        }

        /// <summary>
        /// Returns true if a method name is suffixed with 'Async'.
        /// </summary>
        internal static bool IsAsyncSuffixed(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodName = nodeAnalysisContext.GetMethodName();

            var isAsyncSuffixed = methodName.EndsWith("Async", StringComparison.CurrentCultureIgnoreCase);

            return isAsyncSuffixed;
        }

        /// <summary>
        /// Returns the method name.
        /// </summary>
        internal static string GetMethodName(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax) nodeAnalysisContext.Node;

            var methodName = methodDeclarationSyntax.Identifier.ValueText;

            return methodName;
        }

        /// <summary>
        /// Gets a method/method-like (including constructor, destructor, operator, or property/event accessor) symbol.
        /// </summary>
        private static IMethodSymbol GetMethodSymbol(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var methodSymbol = nodeAnalysisContext
                .SemanticModel
                .GetDeclaredSymbol(
                    methodDeclarationSyntax,
                    nodeAnalysisContext.CancellationToken
                );

            return methodSymbol;
        }

        /// <summary>
        /// Gets any method attributes.
        /// </summary>
        private static List<string> GetMethodAttributes(
            this MethodDeclarationSyntax methodDeclarationSyntax
        )
        {
            var methodAttributes = methodDeclarationSyntax
                .AttributeLists
                .SelectMany(attrListSyntax => attrListSyntax.Attributes)
                .Select(attribute => attribute.Name.TryGetInferredMemberName() ?? string.Empty)
                .ToList();

            return methodAttributes;
        }

        /// <summary>
        /// Determines whether a methods containing type is a controller base type 
        /// </summary>
        private static bool IsControllerBaseType(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var classBaseType = nodeAnalysisContext
                .SemanticModel
                .GetDeclaredSymbol(nodeAnalysisContext.Node)
                .ContainingType
                .BaseType;

            if (classBaseType == null)
            {
                return false;
            }

            var isControllerBaseType = classBaseType
                .Name
                .Equals(
                    "ControllerBase",
                    StringComparison.InvariantCultureIgnoreCase
                );

            return isControllerBaseType;
        }
    }
}