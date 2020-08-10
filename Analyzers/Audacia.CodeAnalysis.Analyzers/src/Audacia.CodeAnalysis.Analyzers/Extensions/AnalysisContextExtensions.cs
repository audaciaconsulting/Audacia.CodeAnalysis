using System;
using System.Linq;
using Microsoft.CodeAnalysis;
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
    }
}
