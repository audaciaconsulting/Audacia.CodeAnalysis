using Microsoft.CodeAnalysis;

namespace Audacia.CodeAnalysis.Analyzers.Shared.Settings;

/// <summary>
/// Implementation of <see cref="IAdditionalText"/> wrapping the <see cref="AdditionalText"/> class.
/// </summary>
public class AdditionalTextFacade(AdditionalText additionalText) : IAdditionalText
{
    public string Path => additionalText.Path;

    public string FindRuleSettingValue(SettingsKey key)
    {
        var sourceText = additionalText.GetText();
        var match = sourceText.Lines.FirstOrDefault(line => line.ToString().StartsWith(key.ToString()));
        if (match == default)
        {
            return string.Empty;
        }

        return match.ToString();
    }
}
