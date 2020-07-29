using System;
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
    }
}
