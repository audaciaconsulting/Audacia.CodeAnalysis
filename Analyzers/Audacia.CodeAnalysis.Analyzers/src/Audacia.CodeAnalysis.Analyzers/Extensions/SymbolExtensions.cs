using System.Linq;
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
    }
}
