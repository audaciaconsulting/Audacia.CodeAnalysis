using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    internal static class SyntaxTreeExtensions
    {
        public static bool ContainsUsingAlias(this SyntaxTree tree, ConcurrentDictionary<SyntaxTree, bool> cache)
        {
            if (tree == null)
            {
                return false;
            }

            if (cache.ContainsKey(tree))
            {
                return true;
            }

            var isGenerated = ContainsUsingAliasNoCache(tree);
            cache.TryAdd(tree, isGenerated);
            return isGenerated;
        }

        private static bool ContainsUsingAliasNoCache(SyntaxTree tree)
        {
            var nodes = tree.GetRoot().DescendantNodes(node => node.IsKind(SyntaxKind.CompilationUnit) || node.IsKind(SyntaxKind.NamespaceDeclaration));
            return nodes.OfType<UsingDirectiveSyntax>().Any(x => x.Alias != null);
        }
    }
}