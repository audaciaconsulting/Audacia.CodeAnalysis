using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.EditorConfigSettings
{
    internal class SettingsReader
    {
        private const string EditorConfigFileName = ".editorconfig";

        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

        public SettingsReader(AnalyzerOptions options, CancellationToken cancellationToken)
        {
            _analyzerConfigOptionsProvider = options.AnalyzerConfigOptionsProvider;
        }

        internal int? TryGetInt(SyntaxTree syntaxTree, SettingsKey key)
        {
            string textValue = TryGetValue(syntaxTree, key);

            if (textValue != null)
            {
                if (int.TryParse(textValue, out int value))
                {
                    return value;
                }

                throw new InvalidConfigException($"Value for '{key.ToString().ToLowerInvariant()}' in '{EditorConfigFileName}' must be an integer.");
            }

            return null;
        }

        internal string TryGetValue(SyntaxTree syntaxTree, SettingsKey key)
        {
            var options = _analyzerConfigOptionsProvider.GetOptions(syntaxTree);
            var isFound = options.TryGetValue(key.ToString(), out var value);

            return isFound ? value : null;
        }
    }
}
