using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    internal class EditorConfigSettingsReader : ISettingsReader
    {
        private const string EditorConfigFileName = ".editorconfig";

        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

        public EditorConfigSettingsReader(AnalyzerOptions options)
        {
            _analyzerConfigOptionsProvider = options.AnalyzerConfigOptionsProvider;
        }

        public int? TryGetInt(SyntaxTree syntaxTree, SettingsKey key)
        {
            string textValue = TryGetValue(syntaxTree, key);

            if (textValue != null)
            {
                if (int.TryParse(textValue, out var value))
                {
                    return value;
                }

                throw new InvalidConfigException($"Value for '{key.ToString().ToLowerInvariant()}' in '{EditorConfigFileName}' must be an integer.");
            }

            return null;
        }

        public string TryGetValue(SyntaxTree syntaxTree, SettingsKey key)
        {
            var options = _analyzerConfigOptionsProvider.GetOptions(syntaxTree);
            var isFound = options.TryGetValue(key.ToString(), out var value);

            return isFound ? value : null;
        }

        public bool? TryGetBool(SyntaxTree syntaxTree, SettingsKey key)
        {
            string textValue = TryGetValue(syntaxTree, key);

            if (textValue != null)
            {
                if (bool.TryParse(textValue, out var value))
                {
                    return value;
                }

                throw new InvalidConfigException($"Value for '{key.ToString().ToLowerInvariant()}' in '{EditorConfigFileName}' must be an integer.");
            }

            return null;
        }
    }
}
