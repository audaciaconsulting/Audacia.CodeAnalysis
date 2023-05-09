using System;
using System.Collections.Generic;
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
        private static readonly Lazy<IEqualityComparer<ISymbol>> SymbolComparerLazy = new Lazy<IEqualityComparer<ISymbol>>(() =>
        {
            Type comparerType = typeof(ISymbol).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.SymbolEqualityComparer");
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

        public static bool IsSynthesized(this ISymbol symbol)
        {
            return !symbol.Locations.Any();
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

            var matchingSyntax = syntax.IsKind(SyntaxKind.RecordDeclaration) || syntax.IsKind(SyntaxKind.RecordStructDeclaration);
            var containsModifiers = modifiers != null && modifiers.Value.Any(modifier => modifier.IsKind(SyntaxKind.RecordKeyword));

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
                   (methodSymbol.MethodKind == MethodKind.LambdaMethod || methodSymbol.MethodKind == MethodKind.AnonymousFunction);
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

            var isControllerAction = attributes.Any(attribute => controllerActionAttributeNames.Any(name => attribute.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)));

            return isControllerAction;
        }
    }
}
