using System.Collections.Generic;

namespace Audacia.CodeAnalysis.Analyzers.Settings
{
    public class AdditionalFilesReader
    {
        private readonly ICollection<IAdditionalText> _additionalFiles;

        public AdditionalFilesReader(ICollection<IAdditionalText> additionalFiles)
        {
            _additionalFiles = additionalFiles;
        }

        public string TryGetValue(SettingsKey key)
        {
            string value = null;
            foreach (var file in _additionalFiles)
            {
                var match = file.FindRuleSettingValue(key);
                if (match != null)
                {
                    // Found rule - need to split on '=' to get the value
                    value = match.Split('=')[1].Trim();
                    break;
                }
            }

            return value;
        }
    }
}
