using System.Linq;
using Microsoft.CodeAnalysis;

namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    /// <summary>
    /// Implementation of <see cref="IAdditionalText"/> wrapping the <see cref="AdditionalText"/> class.
    /// </summary>
    public class AdditionalTextFacade : IAdditionalText
    {
        private readonly AdditionalText _additionalText;

        public string Path => _additionalText.Path;

        public AdditionalTextFacade(AdditionalText additionalText)
        {
            _additionalText = additionalText;
        }

        public string FindRuleSettingValue(SettingsKey key)
        {
            var sourceText = _additionalText.GetText();
            var match = sourceText.Lines.FirstOrDefault(line => line.ToString().StartsWith(key.ToString()));
            if (match == default)
            {
                return null;
            }

            return match.ToString();
        }
    }
}
