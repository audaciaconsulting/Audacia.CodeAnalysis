namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings
{
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string message)
            : base(message)
        {

        }
    }
}
