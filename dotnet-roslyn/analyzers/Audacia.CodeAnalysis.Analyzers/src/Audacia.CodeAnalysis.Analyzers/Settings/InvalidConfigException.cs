using System;

namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string message)
            : base(message)
        {

        }
    }
}
