using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    /// <summary>
    /// Extensions to the <see cref="ISymbol"/> type.
    /// </summary>
    internal static class SymbolExtensions
    {
        private static readonly Lazy<IEqualityComparer<ISymbol>> SymbolComparerLazy =
            new Lazy<IEqualityComparer<ISymbol>>(() =>
            {
                Type comparerType = typeof(ISymbol).GetTypeInfo().Assembly
                    .GetType("Microsoft.CodeAnalysis.SymbolEqualityComparer");
                FieldInfo includeField = comparerType?.GetTypeInfo().GetDeclaredField("IncludeNullability");

                if (includeField != null && includeField.GetValue(null) is IEqualityComparer<ISymbol> comparer)
                {
                    return comparer;
                }

                return EqualityComparer<ISymbol>.Default;
            });

        /// <summary>
        /// Gets a user-friendly string representation of the <see cref="SymbolKind"/>.
        /// </summary>
        /// <param name="symbol"></param>
        public static string GetKind(this ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.Local)
            {
                return "Variable";
            }

            if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
            {
                return GetMethodKind(method);
            }

            return symbol.Kind.ToString();
        }

        /// <summary>
        /// Checks if the symbol or any of its containing types are private, returning false if so.
        /// This helps determine if a parameter is in a publicly visible method.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns>True if the symbol is accessible from the root; otherwise, false.</returns>
        public static bool IsSymbolAccessibleFromRoot(this ISymbol symbol)
        {
            ISymbol container = symbol;

            while (container != null)
            {
                if (container.DeclaredAccessibility == Accessibility.Private)
                {
                    return false;
                }

                container = container.ContainingType;
            }

            return true;
        }

        private static string GetMethodKind(IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                {
                    return "Property accessor";
                }
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                {
                    return "Event accessor";
                }
                case MethodKind.LocalFunction:
                {
                    return "Local function";
                }
                default:
                {
                    return method.Kind.ToString();
                }
            }
        }
        
        public static bool IsBooleanOrNullableBoolean(this ITypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Boolean || IsNullableBoolean(type);
        }
        
        public static bool IsNullableBoolean(this ITypeSymbol type)
        {
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var namedTypeSymbol = type as INamedTypeSymbol;

                if (namedTypeSymbol?.TypeArguments[0].SpecialType == SpecialType.System_Boolean)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSynthesized(this ISymbol symbol)
        {
            return !symbol.Locations.Any();
        }

        /// <summary>
        /// Determines whether the specified symbol is a method named Deconstruct, which is commonly used in C# records and tuples.
        /// Deconstructors are a special case that should not trigger the diagnostic.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns><c>true</c> if the symbol is a deconstructor method; otherwise, <c>false</c>.</returns>
        public static bool IsDeconstructor(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                var methodSymbol = (IMethodSymbol)symbol;
                return methodSymbol.Name == "Deconstruct";
            }

            return false;
        }

        public static bool IsPropertyOrEventAccessor(this ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;

            switch (method?.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        public static bool IsInterfaceImplementation<TSymbol>(this TSymbol member)
            where TSymbol : ISymbol
        {
            if (!(member is IFieldSymbol))
            {
                foreach (TSymbol interfaceMember in member.ContainingType.AllInterfaces.SelectMany(@interface =>
                             @interface.GetMembers().OfType<TSymbol>()))
                {
                    ISymbol implementer = member.ContainingType.FindImplementationForInterfaceMember(interfaceMember);

                    if (member.Equals(implementer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HidesBaseMember(this ISymbol member, CancellationToken cancellationToken)
        {
            foreach (SyntaxReference reference in member.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = reference.GetSyntax(cancellationToken);
                SyntaxTokenList? modifiers = TryGetModifiers(syntax);

                if (ContainsNewModifier(modifiers))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRecordImplementation(this ISymbol member, CancellationToken cancellationToken)
        {
            foreach (SyntaxReference reference in member.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = reference.GetSyntax(cancellationToken);

                if (ContainsRecordSyntax(syntax))
                {
                    return true;
                }
            }

            return false;
        }

        private static SyntaxTokenList? TryGetModifiers(SyntaxNode syntax)
        {
            switch (syntax)
            {
                case MethodDeclarationSyntax methodSyntax:
                {
                    return methodSyntax.Modifiers;
                }
                case BasePropertyDeclarationSyntax propertyEventIndexerSyntax:
                {
                    return propertyEventIndexerSyntax.Modifiers;
                }
                case VariableDeclaratorSyntax _:
                {
                    if (syntax.Parent.Parent is BaseFieldDeclarationSyntax eventFieldSyntax)
                    {
                        return eventFieldSyntax.Modifiers;
                    }

                    break;
                }
                case BaseTypeDeclarationSyntax typeSyntax:
                {
                    return typeSyntax.Modifiers;
                }
                case DelegateDeclarationSyntax delegateSyntax:
                {
                    return delegateSyntax.Modifiers;
                }
            }

            return null;
        }

        private static bool ContainsNewModifier(SyntaxTokenList? modifiers)
        {
            return modifiers != null && modifiers.Value.Any(modifier => modifier.IsKind(SyntaxKind.NewKeyword));
        }

        private static bool ContainsRecordSyntax(SyntaxNode syntax)
        {
            SyntaxTokenList? modifiers = TryGetModifiers(syntax);

            var matchingSyntax = syntax.IsKind(SyntaxKind.RecordDeclaration) ||
                                 syntax.IsKind(SyntaxKind.RecordStructDeclaration);
            var containsModifiers = modifiers != null &&
                                    modifiers.Value.Any(modifier => modifier.IsKind(SyntaxKind.RecordKeyword));

            return matchingSyntax || containsModifiers;
        }

        public static ITypeSymbol GetSymbolType(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol field:
                {
                    return field.Type;
                }
                case IPropertySymbol property:
                {
                    return property.Type;
                }
                case IEventSymbol @event:
                {
                    return @event.Type;
                }
                case IMethodSymbol method:
                {
                    return method.ReturnType;
                }
                case IParameterSymbol parameter:
                {
                    return parameter.Type;
                }
                case ILocalSymbol local:
                {
                    return local.Type;
                }
                default:
                {
                    throw new InvalidOperationException($"Unexpected type '{symbol.GetType()}'.");
                }
            }
        }

        public static bool IsEqualTo(this ISymbol first, ISymbol second)
        {
            return SymbolComparerLazy.Value.Equals(first, second);
        }

        public static bool IsLambdaExpressionParameter(this IParameterSymbol parameter)
        {
            return parameter.ContainingSymbol is IMethodSymbol methodSymbol &&
                   (methodSymbol.MethodKind == MethodKind.LambdaMethod ||
                    methodSymbol.MethodKind == MethodKind.AnonymousFunction);
        }

        public static SyntaxNode TryGetBodySyntaxForMethod(this IMethodSymbol method,
            CancellationToken cancellationToken)
        {
            foreach (SyntaxNode syntaxNode in method.DeclaringSyntaxReferences
                         .Select(syntaxReference => syntaxReference.GetSyntax(cancellationToken)).ToArray())
            {
                SyntaxNode bodySyntax = TryGetDeclarationBody(syntaxNode);

                if (bodySyntax != null)
                {
                    return bodySyntax;
                }
            }

            return TryGetBodyForPartialMethodSyntax(method, cancellationToken);
        }

        private static SyntaxNode TryGetDeclarationBody(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case BaseMethodDeclarationSyntax methodSyntax:
                {
                    return (SyntaxNode)methodSyntax.Body ?? methodSyntax.ExpressionBody?.Expression;
                }
                case AccessorDeclarationSyntax accessorSyntax:
                {
                    return (SyntaxNode)accessorSyntax.Body ?? accessorSyntax.ExpressionBody?.Expression;
                }
                case PropertyDeclarationSyntax propertySyntax:
                {
                    return propertySyntax.ExpressionBody?.Expression;
                }
                case IndexerDeclarationSyntax indexerSyntax:
                {
                    return indexerSyntax.ExpressionBody?.Expression;
                }
                case AnonymousFunctionExpressionSyntax anonymousFunctionSyntax:
                {
                    return anonymousFunctionSyntax.Body;
                }
                case LocalFunctionStatementSyntax localFunctionSyntax:
                {
                    return (SyntaxNode)localFunctionSyntax.Body ?? localFunctionSyntax.ExpressionBody?.Expression;
                }
                default:
                {
                    return null;
                }
            }
        }

        private static SyntaxNode TryGetBodyForPartialMethodSyntax(IMethodSymbol method,
            CancellationToken cancellationToken)
        {
            return method.PartialImplementationPart != null
                ? TryGetBodySyntaxForMethod(method.PartialImplementationPart, cancellationToken)
                : null;
        }

        public static IOperation TryGetOperationBlockForMethod(this IMethodSymbol method, Compilation compilation,
            CancellationToken cancellationToken)
        {
            SyntaxNode bodySyntax = TryGetBodySyntaxForMethod(method, cancellationToken);

            if (bodySyntax != null)
            {
                SemanticModel model = compilation.GetSemanticModel(bodySyntax.SyntaxTree);
                IOperation operation = model.GetOperation(bodySyntax);

                if (operation != null && !operation.HasErrors(compilation, cancellationToken))
                {
                    return operation;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if method is representing a http action (e.g. get/post/delete/put etc)
        /// </summary>
        internal static bool IsControllerAction(this IMethodSymbol method)
        {
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

            var attributes = method.GetAttributes().Select(attribute => attribute.AttributeClass.Name.ToString());

            var isControllerAction = attributes.Any(attribute =>
                controllerActionAttributeNames.Any(name =>
                    attribute.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)));

            return isControllerAction;
        }

        /// <summary>
        /// Checks whether the <paramref name="parameterSymbol"/> is the last parameter and returns true is so.
        /// </summary>
        /// <param name="parameterSymbol"></param>
        /// <param name="methodParameters"></param>
        /// <returns></returns>
        public static bool IsLastParameter(this IParameterSymbol parameterSymbol,
            ImmutableArray<IParameterSymbol> methodParameters)
        {
            // If the list is empty, then there's no last parameter
            if (methodParameters.Length == 0)
            {
                return false;
            }

            // Get the last parameter
            var lastParameter = methodParameters[methodParameters.Length - 1];

            // Check if the provided parameterSymbol is the last one
            return ReferenceEquals(lastParameter, parameterSymbol);
        }

        /// <summary>
        /// Return a value indicating whether the provided <paramref name="symbol"/> is a constructor.
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns></returns>
        public static bool IsConstructor(this ISymbol symbol)
        {
            return symbol is IMethodSymbol methodSymbol
                   && (methodSymbol.MethodKind == MethodKind.Constructor ||
                       methodSymbol.MethodKind == MethodKind.StaticConstructor);
        }

        /// <summary>
        /// Return a value indicating whether the provided symbol represents a primary constructor.
        /// <br/>
        /// <example>
        /// <c>public class MyClass(int property)...</c> the <c>MyClass</c> method symbol would return true.
        /// </example>
        /// </summary>
        /// <param name="symbol">The symbol to check.</param>
        /// <returns></returns>
        public static bool IsPrimaryConstructor(this ISymbol symbol)
        {
            // Code here is inspired by Roslyn's internal extensions https://github.com/dotnet/roslyn/blob/63d2b774f2984b686b1c82697ef3bf446e8d8133/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Extensions/ITypeSymbolExtensions.cs#L40
            if (symbol.IsConstructor()
                && symbol.ContainingSymbol is INamedTypeSymbol typeSymbol
                && (typeSymbol.TypeKind is TypeKind.Class || typeSymbol.TypeKind is TypeKind.Struct))
            {
                // Currently, the Roslyn API doesn't provide information about primary constructors.
                // This is tracked by: https://github.com/dotnet/roslyn/issues/53092.
                var primaryConstructor = typeSymbol.InstanceConstructors.FirstOrDefault(
                    c =>
                    {
                        var syntaxNode = c.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        return syntaxNode is RecordDeclarationSyntax
                               || syntaxNode is ClassDeclarationSyntax
                               || syntaxNode is StructDeclarationSyntax;
                    });
                return primaryConstructor != null;
            }

            return false;
        }
    }
}