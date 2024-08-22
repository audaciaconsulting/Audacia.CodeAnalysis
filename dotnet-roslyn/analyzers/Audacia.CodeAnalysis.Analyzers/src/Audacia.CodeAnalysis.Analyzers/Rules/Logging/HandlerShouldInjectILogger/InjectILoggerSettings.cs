using System.Collections.Generic;

namespace Audacia.CodeAnalysis.Analyzers.Rules.Logging.HandlerShouldInjectILogger
{
    public class InjectILoggerSettings
    {
        public IEnumerable<string> HandlerEndingIdentifiers { get; }

        public InjectILoggerSettings(IEnumerable<string> handlerEndingIdentifiers)
        {
            HandlerEndingIdentifiers = handlerEndingIdentifiers;
        }
    }
}