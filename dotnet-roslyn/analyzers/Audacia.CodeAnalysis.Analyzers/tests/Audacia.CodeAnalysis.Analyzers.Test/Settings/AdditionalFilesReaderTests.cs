using System.Collections.Generic;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Audacia.CodeAnalysis.Analyzers.Test.Settings
{
    [TestClass]
    public class AdditionalFilesReaderTests
    {
        private static AdditionalFilesReader SetupReader(SettingsKey key, params (string path, string settingsValue)[] fileSetups)
        {
            var substituteFiles = new List<IAdditionalText>();
            foreach (var (path, settingsValue) in fileSetups)
            {
                var substituteFile = Substitute.For<IAdditionalText>();
                substituteFile.Path.Returns(path);

                if (settingsValue != null)
                {
                    var setting = $"{key} = {settingsValue}";
                    substituteFile.FindRuleSettingValue(key).Returns(setting);
                }
                else
                {
                    // Setting not in this file (the substitute would otherwise return an empty string)
                    substituteFile.FindRuleSettingValue(default).ReturnsNullForAnyArgs();
                }

                substituteFiles.Add(substituteFile);
            }
            
            return new AdditionalFilesReader(substituteFiles);
        }

        [TestMethod]
        public void Value_found_when_present_in_single_file()
        {
            var key = new SettingsKey("abc", "name");
            const string expectedValue = "123";
            var reader = SetupReader(key, ("dummy-path", expectedValue));

            var actualValue = reader.TryGetValue(key);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void Value_found_when_present_in_second_file()
        {
            var key = new SettingsKey("abc", "name");
            const string expectedValue = "123";
            var reader = SetupReader(key, ("dummy-path", null), ("dummy-path-2", expectedValue));

            var actualValue = reader.TryGetValue(key);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void Value_not_searched_for_in_second_file_if_found_in_first()
        {
            var key = new SettingsKey("abc", "name");
            const string expectedValue = "123";

            var substituteFileOne = Substitute.For<IAdditionalText>();
            substituteFileOne.Path.Returns("dummy-path");
            substituteFileOne.FindRuleSettingValue(key).Returns($"{key} = {expectedValue}");

            var substituteFileTwo = Substitute.For<IAdditionalText>();
            substituteFileTwo.Path.Returns("dummy-path-2");

            var reader = new AdditionalFilesReader(new[] { substituteFileOne, substituteFileTwo });

            reader.TryGetValue(key);

            substituteFileTwo.DidNotReceive().FindRuleSettingValue(key);
        }

        [TestMethod]
        public void Whitespace_excluded_from_value_returned()
        {
            var key = new SettingsKey("abc", "name");
            const string expectedValue = "123";

            var substituteFile = Substitute.For<IAdditionalText>();
            substituteFile.Path.Returns("dummy-path");
            substituteFile.FindRuleSettingValue(key).Returns($"{key} = {expectedValue}  ");

            var reader = new AdditionalFilesReader(new[] { substituteFile });

            var actualValue = reader.TryGetValue(key);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void Null_value_returned_if_setting_malformed()
        {
            var key = new SettingsKey("abc", "name");

            var substituteFile = Substitute.For<IAdditionalText>();
            substituteFile.Path.Returns("dummy-path");
            // Malformed means no '=' sign
            substituteFile.FindRuleSettingValue(key).Returns($"{key}  123");

            var reader = new AdditionalFilesReader(new[] { substituteFile });

            var actualValue = reader.TryGetValue(key);

            Assert.IsNull(actualValue);
        }
    }
}
