using System.Collections.Generic;

namespace Audacia.CodeAnalysis.Analyzers.Rules.Observability.HandlerShouldInjectILogger
{
    public class InjectILoggerSettings
    {
        public IEnumerable<string> HandlerIdentifyingTerms { get; }

        public InjectILoggerSettings(IEnumerable<string> handlerIdentifyingTerms)
        {
            HandlerIdentifyingTerms = handlerIdentifyingTerms;
        }
    }
}