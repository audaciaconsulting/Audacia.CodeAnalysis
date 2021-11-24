using System.Collections.Generic;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Settings
{
    [TestClass]
    public class AdditionalFilesReaderTests
    {
        private static AdditionalFilesReader SetupReader(SettingsKey key, params (string path, string settingsValue)[] fileSetups)
        {
            var mockFiles = new List<IAdditionalText>();
            foreach (var (path, settingsValue) in fileSetups)
            {
                var mockFile = new Mock<IAdditionalText>();
                mockFile.SetupGet(f => f.Path).Returns(path);

                if (settingsValue != null)
                {
                    var setting = $"{key} = {settingsValue}";
                    mockFile.Setup(f => f.FindRuleSettingValue(key)).Returns(setting);
                }

                mockFiles.Add(mockFile.Object);
            }
            
            return new AdditionalFilesReader(mockFiles);
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

            var mockFileOne = new Mock<IAdditionalText>();
            mockFileOne.SetupGet(f => f.Path).Returns("dummy-path");
            mockFileOne.Setup(f => f.FindRuleSettingValue(key)).Returns($"{key} = {expectedValue}");

            var mockFileTwo = new Mock<IAdditionalText>();
            mockFileTwo.SetupGet(f => f.Path).Returns("dummy-path-2");

            var reader = new AdditionalFilesReader(new[] { mockFileOne.Object, mockFileTwo.Object });

            reader.TryGetValue(key);

            mockFileTwo.Verify(f => f.FindRuleSettingValue(key), Times.Never);
        }

        [TestMethod]
        public void Whitespace_excluded_from_value_returned()
        {
            var key = new SettingsKey("abc", "name");
            const string expectedValue = "123";

            var mockFile = new Mock<IAdditionalText>();
            mockFile.SetupGet(f => f.Path).Returns("dummy-path");
            mockFile.Setup(f => f.FindRuleSettingValue(key)).Returns($"{key} = {expectedValue}  ");

            var reader = new AdditionalFilesReader(new[] { mockFile.Object });

            var actualValue = reader.TryGetValue(key);

            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void Null_value_returned_if_setting_malformed()
        {
            var key = new SettingsKey("abc", "name");

            var mockFile = new Mock<IAdditionalText>();
            mockFile.SetupGet(f => f.Path).Returns("dummy-path");
            // Malformed means no '=' sign
            mockFile.Setup(f => f.FindRuleSettingValue(key)).Returns($"{key}  123");

            var reader = new AdditionalFilesReader(new[] { mockFile.Object });

            var actualValue = reader.TryGetValue(key);

            Assert.IsNull(actualValue);
        }
    }
}
