using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audacia.CodeAnalysis.Analyzers.Extensions
{
    internal static class SyntaxNodeAnalysisContextExtensions
    {
        public static SymbolAnalysisContext ToSymbolContext(this SyntaxNodeAnalysisContext syntaxContext)
        {
            ISymbol symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
            return SyntaxToSymbolContext(syntaxContext, symbol);
        }

        private static SymbolAnalysisContext SyntaxToSymbolContext(SyntaxNodeAnalysisContext syntaxContext, ISymbol symbol)
        {

#pragma warning disable CS0618 // Type or member is obsolete
            return new SymbolAnalysisContext(symbol, syntaxContext.SemanticModel.Compilation, syntaxContext.Options, syntaxContext.ReportDiagnostic, _ => true,
                syntaxContext.CancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
