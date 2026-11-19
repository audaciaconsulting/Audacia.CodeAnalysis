namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings;

/// <summary>
/// Class to read custom rule settings from 'additional files'.
/// </summary>
public class AdditionalFilesReader(ICollection<IAdditionalText> additionalFiles)
{
    /// <summary>
    /// Tries to get the setting value for the given <paramref name="key"/> from the loaded additional files.
    /// </summary>
    /// <param name="key">The <see cref="SettingsKey"/> to search for.</param>
    /// <returns>The value for the setting if one is found, otherwise <see langword="null"/>.</returns>
    public string TryGetValue(SettingsKey key)
    {
        string value = null;

        foreach (var file in additionalFiles)
        {
            var match = file.FindRuleSettingValue(key);
            if (!string.IsNullOrEmpty(match))
            {
                // Found rule - need to split on '=' to get the value
                var splitMatch = match.Split('=');
                if (splitMatch.Length > 1)
                {
                    value = splitMatch[1].Trim();
                    break;
                }
            }
        }

        return value;
    }
}
