using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    internal static class SyntaxNodeAnalysisContextExtensions
    {
        /// <summary>
        /// Converts a <see cref="SyntaxNodeAnalysisContext"/> to a <see cref="SymbolAnalysisContext"/>.
        /// By extracting the declared symbol from the syntax node it easier to deal with parameters as symbols rather than syntax nodes.
        /// </summary>
        /// <param name="syntaxContext">The syntax node analysis context.</param>
        /// <returns>A symbol analysis context.</returns>
        public static SymbolAnalysisContext ToSymbolContext(this SyntaxNodeAnalysisContext syntaxContext)
        {
            ISymbol symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
            return SyntaxToSymbolContext(syntaxContext, symbol);
        }

        /// <summary>
        /// Creates a <see cref="SymbolAnalysisContext"/> from a <see cref="SyntaxNodeAnalysisContext"/> and a symbol.
        /// </summary>
        /// <param name="syntaxContext">The syntax node analysis context.</param>
        /// <param name="symbol">The symbol.</param>
        /// <returns>A symbol analysis context.</returns>
        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext syntaxContext, ISymbol symbol)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new SymbolAnalysisContext(symbol, syntaxContext.SemanticModel.Compilation, syntaxContext.Options, syntaxContext.ReportDiagnostic, _ => true,
                syntaxContext.CancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
