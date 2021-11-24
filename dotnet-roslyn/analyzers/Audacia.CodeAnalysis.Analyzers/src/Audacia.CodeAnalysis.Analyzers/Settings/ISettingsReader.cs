using Microsoft.CodeAnalysis;

namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    public interface ISettingsReader
    {
        int? TryGetInt(SyntaxTree syntaxTree, SettingsKey key);

        string TryGetValue(SyntaxTree syntaxTree, SettingsKey key);

        bool? TryGetBool(SyntaxTree syntaxTree, SettingsKey key);
    }
}
