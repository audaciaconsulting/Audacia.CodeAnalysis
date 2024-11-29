using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Audacia.CodeAnalysis.Analyzers.Rules.Observability.HandlerShouldInjectILogger
{
    public class HandlerShouldInjectILoggerInfo
    {
        private readonly InjectILoggerSettings _settings;

        public IMethodSymbol MethodSymbol { get; }

        public IEnumerable<string> HandlerIdentifyingTerms => _settings.HandlerIdentifyingTerms;

        public HandlerShouldInjectILoggerInfo(
            IMethodSymbol methodSymbol,
            InjectILoggerSettings settings)
        {
            MethodSymbol = methodSymbol;
            _settings = settings;
        }
    }
}