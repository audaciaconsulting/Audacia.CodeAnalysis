using System.Linq;
using Microsoft.CodeAnalysis;

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
    }
}
