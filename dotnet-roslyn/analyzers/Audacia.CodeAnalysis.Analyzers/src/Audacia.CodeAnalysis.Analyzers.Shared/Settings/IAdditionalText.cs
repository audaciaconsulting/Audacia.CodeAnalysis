namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings;

/// <summary>
/// Abstraction of the <see cref="Microsoft.CodeAnalysis.AdditionalText"/> class.
/// </summary>
public interface IAdditionalText
{
    /// <summary>
    /// Gets the path to the file.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Finds the line for the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The matching line, or an empty string if no match is found.</returns>
    string FindRuleSettingValue(SettingsKey key);
}
