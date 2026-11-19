using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Shared.Extensions;

public static class AnalysisContextExtensions
{
    public static void SkipEmptyName(this SymbolAnalysisContext context, Action<SymbolAnalysisContext> action)
    {
        if (!string.IsNullOrEmpty(context.Symbol.Name))
        {
            action(context);
        }
    }

    public static void SkipEmptyName(
        this SyntaxNodeAnalysisContext context,
        Action<SyntaxNodeAnalysisContext> action)
    {
        if (context.Node is ParameterSyntax parameterSyntax &&
            !string.IsNullOrWhiteSpace(parameterSyntax.Identifier.ValueText))
        {
            action(context);
        }
    }

    public static void SkipInvalid(this OperationAnalysisContext context, Action<OperationAnalysisContext> action)
    {
        if (!context.Operation.HasErrors(context.Compilation, context.CancellationToken))
        {
            action(context);
        }
    }

    public static void SkipInvalid(
        this OperationBlockAnalysisContext context,
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
    public static bool IsAsynchronous(this SyntaxNodeAnalysisContext nodeAnalysisContext)
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
    public static bool IsAwaitable(this SyntaxNodeAnalysisContext nodeAnalysisContext)
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
            .ConvertAll(
                awaitableTypeName =>
                    nodeAnalysisContext
                        .SemanticModel
                        .Compilation
                        .GetTypeByMetadataName(awaitableTypeName)
                        ?.MetadataName);

        return awaitableTypes
            .Any(
                awaitableType => methodReturnType.Equals(awaitableType, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// Returns true if a method name is suffixed with 'Async'.
    /// </summary>
    public static bool IsAsyncSuffixed(this SyntaxNodeAnalysisContext nodeAnalysisContext)
    {
        var methodName = nodeAnalysisContext.GetMethodName();

        return methodName.EndsWith("Async", StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Returns the method name.
    /// </summary>
    public static string GetMethodName(this SyntaxNodeAnalysisContext nodeAnalysisContext)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

        return methodDeclarationSyntax.Identifier.ValueText;
    }

    /// <summary>
    /// Gets a method/method-like (including constructor, destructor, operator, or property/event accessor) symbol.
    /// </summary>
    private static IMethodSymbol GetMethodSymbol(this SyntaxNodeAnalysisContext nodeAnalysisContext)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

        return nodeAnalysisContext
            .SemanticModel
            .GetDeclaredSymbol(
                methodDeclarationSyntax,
                nodeAnalysisContext.CancellationToken);
    }

    /// <summary>
    /// Gets any method attributes.
    /// </summary>
    public static List<string> GetMethodAttributes(
        this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        return [.. methodDeclarationSyntax
            .AttributeLists
            .SelectMany(attrListSyntax => attrListSyntax.Attributes)
            .Select(attribute => attribute.Name.TryGetInferredMemberName() ?? attribute.Name.ToString() ?? string.Empty)];
    }

    public static SyntaxTree GetSyntaxTree(this SyntaxNodeAnalysisContext context)
    {
        return context.Compilation.SyntaxTrees.FirstOrDefault();
    }

    public static SyntaxTree GetSyntaxTree(this OperationAnalysisContext context)
    {
        return context.Compilation.SyntaxTrees.FirstOrDefault();
    }

    public static SyntaxTree GetSyntaxTree(this SymbolAnalysisContext context)
    {
        return context.Compilation.SyntaxTrees.FirstOrDefault();
    }
}