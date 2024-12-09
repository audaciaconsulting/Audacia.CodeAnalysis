using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        internal static void SkipEmptyName(
            this SyntaxNodeAnalysisContext context,
            Action<SyntaxNodeAnalysisContext> action)
        {
            if (context.Node is ParameterSyntax parameterSyntax &&
                !string.IsNullOrWhiteSpace(parameterSyntax.Identifier.ValueText))
            {
                action(context);
            }
        }

        public static void SkipEmptyName(SyntaxNodeAnalysisContext context, Action<SymbolAnalysisContext> action)
        {
            SymbolAnalysisContext symbolContext = context.ToSymbolContext();
            SkipEmptyName(symbolContext, _ => action(symbolContext));
        }

        private static CompilationWithAnalyzers SyntaxToSymbolContext(SyntaxNodeAnalysisContext syntaxContext,
            ISymbol symbol)
        {
            return new CompilationWithAnalyzers(
                syntaxContext.SemanticModel.Compilation,
                new ImmutableArray<DiagnosticAnalyzer>(),
                syntaxContext.Options);
        }

        internal static void SkipInvalid(this OperationAnalysisContext context, Action<OperationAnalysisContext> action)
        {
            if (!context.Operation.HasErrors(context.Compilation, context.CancellationToken))
            {
                action(context);
            }
        }

        internal static void SkipInvalid(this OperationBlockAnalysisContext context,
            Action<OperationBlockAnalysisContext> action)
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

        public static void SafeRegisterSyntaxNodeAction(this AnalysisContext analysisContext, Action<SymbolAnalysisContext> action, params SyntaxKind[] syntaxKinds)
        {
            analysisContext.RegisterSyntaxNodeAction(context => SkipEmptyName(context, action), syntaxKinds);
        }

        /// <summary>
        /// Returns true if a method is an awaitable method.
        /// </summary>
        internal static bool IsAwaitable(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodReturnType = GetMethodSymbol(nodeAnalysisContext).ReturnType.MetadataName;

            var fullyQualifiedAwaitableTypeNames
                = new List<string>
                {
                    typeof(Task).FullName,
                    typeof(Task<>).FullName,
                    typeof(ValueTask).FullName,
                    typeof(ValueTask<>).FullName,
                    // Hardcode IAsyncEnumerable type name as it's only included in .NET Standard 2.1.
                    // It is in Microsoft.Bcl.AsyncInterfaces, but I don't want to add any extra dependencies here.
                    "System.Collections.Generic.IAsyncEnumerable`1"
                };

            var awaitableTypes = fullyQualifiedAwaitableTypeNames
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
        /// Returns true if method is representing a http action (e.g. get/post/delete/put etc)
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
                    attribute =>
                        controllerActionAttributeNames.Any(name => attribute.StartsWith(name))
                );

            var containingTypeIsControllerType = IsControllerBaseType(nodeAnalysisContext);

            var isPublicMethod = IsPublic(methodDeclarationSyntax);

            return (isControllerAction || containingTypeIsControllerType) && isPublicMethod;
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
            var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

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
        internal static List<string> GetMethodAttributes(
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

        internal static SyntaxTree GetSyntaxTree(this SyntaxNodeAnalysisContext context)
        {
            return context.Compilation.SyntaxTrees.FirstOrDefault();
        }

        internal static SyntaxTree GetSyntaxTree(this OperationAnalysisContext context)
        {
            return context.Compilation.SyntaxTrees.FirstOrDefault();
        }

        internal static SyntaxTree GetSyntaxTree(this SymbolAnalysisContext context)
        {
            return context.Compilation.SyntaxTrees.FirstOrDefault();
        }

        /// <summary>
        /// Checks whether the method has a public modifier.
        /// </summary>
        private static bool IsPublic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var isPublic = methodDeclarationSyntax
                .Modifiers
                .Any(
                    modifier =>
                        modifier.ValueText.Equals("public", StringComparison.InvariantCultureIgnoreCase)
                );

            return isPublic;
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

            var controllerBaseTypeNames
                = new List<string>
                {
                    "Controller",
                    "ControllerBase"
                };

            var isControllerBaseType = controllerBaseTypeNames
                .Any(
                    controllerBaseTypeName => classBaseType
                        .Name
                        .Equals(
                            controllerBaseTypeName,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                );

            return isControllerBaseType;
        }
    }
}