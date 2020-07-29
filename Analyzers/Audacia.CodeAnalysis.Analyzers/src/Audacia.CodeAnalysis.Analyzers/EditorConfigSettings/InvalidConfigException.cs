using System;

namespace Audacia.CodeAnalysis.Analyzers.EditorConfigSettings
{
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string message)
            : base(message)
        {

        }
    }
}
