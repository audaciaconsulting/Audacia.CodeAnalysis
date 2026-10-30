using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings
{
    public class EditorConfigSettingsReader : ISettingsReader
    {
        private const string EditorConfigFileName = ".editorconfig";

        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;
        private readonly AdditionalFilesReader _additionalFilesReader;

        public EditorConfigSettingsReader(AnalyzerOptions options)
        {
            _analyzerConfigOptionsProvider = options.AnalyzerConfigOptionsProvider;
            _additionalFilesReader = new AdditionalFilesReader(
                options.AdditionalFiles
                    .Where(f => f.Path.EndsWith(EditorConfigFileName))
                    .Select(f => new AdditionalTextFacade(f))
                    .ToArray());
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
            if (!isFound)
            {
                // Rider does not load .editorconfig as the AnalyzerConfigOptionsProvider
                // A workaround is to load .editorconfig files as 'additional files', so we check them here
                value = _additionalFilesReader.TryGetValue(key);
            }

            return value;
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
