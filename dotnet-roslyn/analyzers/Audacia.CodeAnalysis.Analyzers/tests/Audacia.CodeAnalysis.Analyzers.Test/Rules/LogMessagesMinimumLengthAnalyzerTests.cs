using System.Collections.Concurrent;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesMinimumLength;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

/// <summary>
/// Most tests give whether a diagnostic is expected, and the message the analyzer is expected to read, i.e. the message as
/// it's measured (rendered and trimmed). The test checks both, so a test can't pass or fail for the wrong reason (e.g. a
/// message of the right length, but read differently than intended). A diagnostic should be expected if, and only if, the
/// message is shorter than the minimum length, which is also checked, so the test data can't contradict itself.
/// </summary>
[TestClass]
public class LogMessagesMinimumLengthAnalyzerTests : CodeFixVerifier
{
    private const int DefaultMinLength = LogMessagesMinimumLengthAnalyzer.DefaultLogMessageMinimumLength;

    private const string TenSpaces = "          ";

    /// <summary>
    /// Alignment padding is capped at <see cref="LogMessagesMinimumLengthAnalyzer.MaximumSettingValue"/> (100) characters.
    /// </summary>
    private const string CappedPadding =
        TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces + TenSpaces;

    private ISettingsReader _settingsReader;

    /// <summary>
    /// The messages read by the most recently created analyzer, by location. The test base runs the analyzer more than once
    /// on the same compilation, so a message read again at the same location is only recorded once.
    /// </summary>
    private ConcurrentDictionary<Location, string> _messagesRead = new();

    private static DiagnosticResult BuildExpectedResult(int lineNumber, int column, int minLength)
    {
        return new DiagnosticResult
        {
            Id = LogMessagesMinimumLengthAnalyzer.Id,
            Message = $"Log messages should be at least {minLength} characters long. LOG-01.4.",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", lineNumber, column)
            ]
        };
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        var messagesRead = new ConcurrentDictionary<Location, string>();
        _messagesRead = messagesRead;

        return new LogMessagesMinimumLengthAnalyzer(_settingsReader, (message, location) =>
        {
            var recorded = messagesRead.GetOrAdd(location, message);
            Assert.AreEqual(recorded, message, "The same message was read differently when the analyzer ran again.");
        });
    }

    [TestMethod]
    public void No_Diagnostic_For_Empty_Code()
    {
        var test = @"
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
        AssertMessagesRead();
    }

    [TestMethod]
    public void No_Diagnostic_When_No_Log_Used()
    {
        var test = @"
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }
    private void Method()
    {
        var x = 1 + 2;
    }
}";

        VerifyNoDiagnostic(test);
        AssertMessagesRead();
    }

    [TestMethod]
    public void No_Diagnostic_For_Long_Enough_Message()
    {
        const string testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }

    private void Method()
    {
        var logger = (new LoggerFactory()).CreateLogger<ClassName>();
        var x = 1 + 2;
        logger.LogInformation(""1234567890"");
    }
}";
        VerifyNoDiagnostic(testCode);
        AssertMessagesRead("1234567890");
    }

    [TestMethod]
    public void Diagnostic_For_Short_Message()
    {
        const string testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }

    private void Method()
    {
        var logger = (new LoggerFactory()).CreateLogger<ClassName>();
        var x = 1 + 2;
        logger.LogInformation(""123456789"");
    }
}";
        var expected = BuildExpectedResult(14, 31, 10);
        VerifyDiagnostic(testCode, expected);
        AssertMessagesRead("123456789");
    }

    [TestMethod]
    [DataRow("string.Format(\"{0}\", \"123456789\")", true, "123456789")]
    [DataRow("string.Format(\"{0}{1}\", \"1234\", \"56789\")", true, "123456789")]
    [DataRow("string.Format(\"{0}{1}{2}\", \"123\", \"456\", \"789\")", true, "123456789")]
    [DataRow("string.Format(\"{0}{1}{2}{3}\", \"12\", \"34\", \"56\", \"789\")", true, "123456789")]
    [DataRow("string.Format(\"{0}{1}{2}{3}\", \"12\", \"34\", \"56\", \"7890\")", false, "1234567890")]
    [DataRow("string.Format(arg1: \"\", format: \"{0}{0}{1}\", arg0: \"12345\")", false, "1234512345")]
    [DataRow("string.Format(\"{0}{1}\", new object[] { \"1234\", \"56789\" })", true, "123456789")]
    public void String_Format_Uses_Constant_String_Arguments(string expression, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(expression, diagnosticExpected, expectedRead);
    }

    [TestMethod]
    [DataRow("20", true, "1234567890123456789")] // 19 characters, below the overridden minimum of 20
    [DataRow("20", false, "12345678901234567890")]
    [DataRow("20", false, "123456789012345678901")]
    [DataRow("5", true, "1234")]
    [DataRow("5", false, "12345")]
    [DataRow("5", false, "123456")]
    [DataRow("1", true, "")]
    [DataRow("1", false, "1")]
    [DataRow("100", true, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("100", false, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Configured_Minimum_Length_Is_Used(string setting, bool diagnosticExpected, string message)
    {
        ConfigureMinimumLength(setting);

        VerifyMessage($"\"{message}\"", diagnosticExpected, expectedRead: message, minLength: int.Parse(setting));
    }

    [TestMethod]
    [DataRow("not-an-integer")]
    [DataRow("10.5")]
    [DataRow("2147483648")]
    public void Invalid_Minimum_Length_Throws_When_Configuration_Is_Read(string setting)
    {
        ConfigureMinimumLength(setting);
        var key = new SettingsKey(LogMessagesMinimumLengthAnalyzer.Id,
            LogMessagesMinimumLengthAnalyzer.LogMessageMinimumLengthSetting);

        AssertAnalyzerThrowsInvalidConfig(
            $"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be an integer.");
    }

    [TestMethod]
    public void Minimum_Length_Less_Than_One_Throws_When_Configuration_Is_Read()
    {
        ConfigureMinimumLength("0");
        var key = new SettingsKey(LogMessagesMinimumLengthAnalyzer.Id,
            LogMessagesMinimumLengthAnalyzer.LogMessageMinimumLengthSetting);

        AssertAnalyzerThrowsInvalidConfig(
            $"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be at least 1.");
    }

    [TestMethod]
    public void Minimum_Length_Greater_Than_One_Hundred_Throws_When_Configuration_Is_Read()
    {
        ConfigureMinimumLength("101");
        var key = new SettingsKey(LogMessagesMinimumLengthAnalyzer.Id,
            LogMessagesMinimumLengthAnalyzer.LogMessageMinimumLengthSetting);

        AssertAnalyzerThrowsInvalidConfig(
            $"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be at most 100.");
    }

    /// <summary>
    /// Compile-time constant strings are read wherever they're used, whether as the whole message or only part of it.
    /// <c>Messages</c> is a class of constants in the test code, like a centralised set of messages (e.g. for translation).
    /// </summary>
    [TestMethod]
    [DataRow("Messages.Short", "", true, "123456789")]
    [DataRow("Messages.Long", "", false, "1234567890")]
    [DataRow("value", "const string value = \"123456789\";", true, "123456789")]
    [DataRow("value", "const string value = \"1234567890\";", false, "1234567890")]
    [DataRow("\n    value", "const string value =\n    \"1234567890\";", false, "1234567890")]
    [DataRow("\r\n\tvalue", "const string value =\r\n\t\"1234567890\";", false, "1234567890")]
    [DataRow("$\"{Messages.Short}\"", "", true, "123456789")]
    [DataRow("$\"{Messages.Long}\"", "", false, "1234567890")]
    [DataRow("$\"12345678{value}\"", "const string value = \"1\";", true, "123456781")]
    [DataRow("$\"123456789{value}\"", "const string value = \"1\";", false, "1234567891")]
    [DataRow("\"{Value}\", Messages.Short", "", true, "123456789")]
    [DataRow("\"{Value}\", Messages.Long", "", false, "1234567890")]
    [DataRow("value + \"6789\"", "const string value = \"12345\";", true, "123456789")]
    [DataRow("value + \"67890\"", "const string value = \"12345\";", false, "1234567890")]
    [DataRow("string.Concat(Messages.Short, \"\")", "", true, "123456789")]
    [DataRow("string.Concat(Messages.Short, \"0\")", "", false, "1234567890")]
    [DataRow("string.Format(\"{0}\", Messages.Short)", "", true, "123456789")]
    [DataRow("string.Format(\"{0}\", Messages.Long)", "", false, "1234567890")]
    [DataRow("string.Format(format, \"123456789\")", "const string format = \"{0}\";", true, "123456789")]
    [DataRow("string.Format(format, \"1234567890\")", "const string format = \"{0}\";", false, "1234567890")]
    [DataRow("Messages.Template, input", "", true, "Order")] // "Order {Id}" with a nominal value
    [DataRow("Messages.LongTemplate, input", "", false, "Order processed")] // "Order processed {Id}" with a nominal value
    public void Constant_Strings_Are_Read(string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    /// <summary>
    /// Anything that isn't a compile-time constant string is given the nominal length of zero
    /// (<see cref="LogMessagesMinimumLengthAnalyzer.NominalParameterLength"/>), as the length of its name or source text says
    /// nothing about the length of the value that will be logged. The tests below cover this for the whole message, and for
    /// each way a value can be only part of it.
    /// </summary>
    [TestMethod]
    [DataRow("input", "", true, "")]
    [DataRow("input.Trim()", "", true, "")]
    [DataRow("value", "var value = \"1234567890\";", true, "")]
    public void Non_Constant_Message_Is_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    [TestMethod]
    [DataRow("$\"{input}\"", "", true, "")]
    [DataRow("$\"123456789{input}\"", "", true, "123456789")]
    [DataRow("$\"1234567890{input}\"", "", false, "1234567890")]
    [DataRow("$\"123456789{input:N}\"", "", true, "123456789")]
    [DataRow("$\"{value}\"", "var value = \"1234567890\";", true, "")]
    public void Non_Constant_Interpolated_Values_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    [TestMethod]
    [DataRow("\"123456789\" + input", "", true, "123456789")]
    [DataRow("\"1234567890\" + input", "", false, "1234567890")]
    [DataRow("input + \"123456789\"", "", true, "123456789")]
    [DataRow("input + \"1234567890\"", "", false, "1234567890")]
    [DataRow("input + input", "", true, "")]
    [DataRow("\"12345\" + value", "var value = \"67890\";", true, "12345")]
    public void Non_Constant_Concatenated_Values_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    [TestMethod]
    [DataRow("string.Concat(\"123456789\", input)", "", true, "123456789")]
    [DataRow("string.Concat(\"1234567890\", input)", "", false, "1234567890")]
    [DataRow("string.Join(\", \", \"12345678\", input)", "", true, "12345678,")]
    [DataRow("string.Join(\", \", \"123456789\", input)", "", false, "123456789,")]
    [DataRow("string.Join(\", \", values)", "var values = new[] { \"1234567890\" };", true, "")]
    public void Non_Constant_String_Concat_And_Join_Values_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    /// <summary>
    /// A non-constant format string, or non-constant values (whether passed individually or in an array), are given the
    /// nominal length.
    /// </summary>
    [TestMethod]
    [DataRow("string.Format(\"123456789{0}\", input)", "", true, "123456789")]
    [DataRow("string.Format(\"1234567890{0}\", input)", "", false, "1234567890")]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"123456789{0}\", input)", "", true, "123456789")]
    [DataRow("string.Format(format, \"1234567890\")", "var format = \"{0}\";", true, "")]
    [DataRow("string.Format(\"123456789{0}\", values)", "var values = new object[] { \"1234567890\" };", true, "123456789")]
    [DataRow("string.Format(\"1234567890{0}\", values)", "var values = new object[] { input };", false, "1234567890")]
    [DataRow("string.Format(\"Order processing failed: {0}\", values)", "var values = new object[] { input };", false, "Order processing failed:")]
    [DataRow("string.Format(\"1{0,8}1\", values)", "var values = new object[] { input };", false, "1        1")]
    public void Non_Constant_String_Format_Values_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    /// <summary>
    /// Format items that can't be matched to an argument are given the nominal length, but the format string is still read.
    /// An unmatched brace is measured as written.
    /// </summary>
    [TestMethod]
    [DataRow("string.Format(\"{1}\", \"1234567890\")", "", true, "")]
    [DataRow("string.Format(\"123456789{1}\", \"1234567890\")", "", true, "123456789")]
    [DataRow("string.Format(\"1234567890{1}\", input)", "", false, "1234567890")]
    [DataRow("string.Format(\"Order processing failed: {1}\", input)", "", false, "Order processing failed:")]
    [DataRow("string.Format(\"123456789{x}\", input)", "", true, "123456789")]
    [DataRow("string.Format(\"1234567890{x}\", input)", "", false, "1234567890")]
    [DataRow("string.Format(\"1{x,8}1\", input)", "", false, "1        1")]
    [DataRow("string.Format(\"123456789{1}\", values)", "var values = new object[] { input };", true, "123456789")]
    [DataRow("string.Format(\"12345678{\", input)", "", true, "12345678{")]
    [DataRow("string.Format(\"123456789{\", input)", "", false, "123456789{")]
    public void Unmatched_String_Format_Items_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    [TestMethod]
    [DataRow("\"123456789{Value}\", input", "", true, "123456789")]
    [DataRow("\"1234567890{Value}\", input", "", false, "1234567890")]
    [DataRow("\"{Value}\", value", "var value = \"1234567890\";", true, "")]
    [DataRow("\"{Known} {Unknown}\", \"12345678\", input", "", true, "12345678")]
    [DataRow("\"{Known} {Unknown}\", \"1234567890\", input", "", false, "1234567890")]
    [DataRow("\"123456789{Id}\", values", "var values = new object[] { \"1\" };", true, "123456789")]
    public void Non_Constant_Message_Template_Values_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    /// <summary>
    /// A spread in a collection expression can supply any number of values. A spread of an inline array is expanded, but
    /// any other spread's length isn't known, so the positions after it are given the nominal length, rather than being
    /// matched to the values that follow it. Concatenated values aren't matched by position, so are still read.
    /// </summary>
    [TestMethod]
    [DataRow("\"{First}{Second}\", [..values, \"1234567890\"]", "var values = new object[] { 1, 2 };", true, "")]
    [DataRow("\"{First}{Second}\", [\"1234567890\", ..values]", "var values = new object[] { 1, 2 };", false, "1234567890")]
    [DataRow("\"{First}{Second}\", [..new object[] { \"1234\" }, \"567890\"]", "", false, "1234567890")]
    [DataRow("\"{First}{Second}\", [..new object[] { \"1234\" }, \"56789\"]", "", true, "123456789")]
    [DataRow("\"{{1234567}}\", [..values]", "var values = new object[] { 1 };", true, "{1234567}")]
    [DataRow("string.Format(\"{0}{1}\", [..values, \"1234567890\"])", "var values = new object[] { 1, 2 };", true, "")]
    [DataRow("string.Format(\"{0}{1}\", [..new object[] { \"1234\" }, \"567890\"])", "", false, "1234567890")]
    [DataRow("string.Concat([..values, \"123456789\"])", "var values = new string[] { input };", true, "123456789")]
    [DataRow("string.Concat([..values, \"1234567890\"])", "var values = new string[] { input };", false, "1234567890")]
    public void Values_After_A_Spread_Of_Unknown_Length_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, declaration);
    }

    /// <summary>
    /// Only constant strings are read, so constants of other types are given the nominal length, e.g. "Order {Id}" with
    /// a value of 1234567890 is measured as "Order". Format specifiers have no effect, and null isn't shown as "(null)".
    /// </summary>
    [TestMethod]
    [DataRow("(string)null", true, "")]
    [DataRow("\"Got {Id}\", (object)null", true, "Got")]
    [DataRow("\"Got {Id}\", new object[] { null }", true, "Got")]
    [DataRow("$\"Got {(object)null}\"", true, "Got")]
    [DataRow("string.Format(\"Got {0}\", (object)null)", true, "Got")]
    [DataRow("\"Order {Id}\", 1234567890", true, "Order")]
    [DataRow("\"Done {Success}\", false", true, "Done")]
    [DataRow("$\"Order {1234567890}\"", true, "Order")]
    [DataRow("string.Format(\"Order {0}\", 1234567890)", true, "Order")]
    [DataRow("\"Order {Id:D10}\", 42", true, "Order")]
    [DataRow("$\"Order {42:D10}\"", true, "Order")]
    [DataRow("string.Format(\"Order {0:D10}\", 42)", true, "Order")]
    [DataRow("\"{{literal}} {Id:D9}\", 42", true, "{literal}")]
    public void Constants_That_Are_Not_Strings_Are_Given_The_Nominal_Length(string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead);
    }

    /// <summary>
    /// Without values, the logger doesn't format a template, so its placeholders are logged as written.
    /// </summary>
    [TestMethod]
    [DataRow("\"{UserId}\"", true, "{UserId}")]
    [DataRow("\"Id {UserId}\"", false, "Id {UserId}")]
    public void Template_Without_Values_Is_Measured_As_Written(string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead);
    }

    /// <summary>
    /// An alignment pads a value, whether it's a constant string ("1" here) or given the nominal length.
    /// Padding outside the message is trimmed, but padding between other text counts towards its length.
    /// </summary>
    [TestMethod]
    [DataRow("$\"{value,9}\"", true, "1")] // "        1" before trimming
    [DataRow("$\"{value,-10}\"", true, "1")]
    [DataRow("\"{Value,9}\", value", true, "1")]
    [DataRow("\"{{{Value}}}\", value", true, "{1}")]
    [DataRow("$\"1234567{value,2}\"", true, "1234567 1")]
    [DataRow("$\"12345678{value,2}\"", false, "12345678 1")]
    [DataRow("$\"{value,-2}1234567\"", true, "1 1234567")]
    [DataRow("$\"{value,-2}12345678\"", false, "1 12345678")]
    [DataRow("\"12345678{Value,2}\", value", false, "12345678 1")]
    [DataRow("\"{Value,-2}12345678\", value", false, "1 12345678")]
    [DataRow("string.Format(\"12345678{0,2}\", value)", false, "12345678 1")]
    [DataRow("$\"1{input,8}\"", true, "1")]
    [DataRow("$\"1{input,7}1\"", true, "1       1")]
    [DataRow("$\"1{input,8}1\"", false, "1        1")]
    [DataRow("$\"1{input,-8}1\"", false, "1        1")]
    [DataRow("\"1{Value,8}1\", input", false, "1        1")]
    [DataRow("\"1{Value,8:D2}1\", input", false, "1        1")]
    [DataRow("string.Format(\"1{0,8}1\", input)", false, "1        1")]
    public void Alignment_Pads_Values(string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, "const string value = \"1\";");
    }

    /// <summary>
    /// Large alignments are capped, without changing whether the message is long enough.
    /// </summary>
    [TestMethod]
    [DataRow("$\"{input,999999}\"", true, "")]
    [DataRow("$\"{input,-999999}\"", true, "")]
    [DataRow("$\"1{input,999999}1\"", false, "1" + CappedPadding + "1")]
    [DataRow("\"1{Value,999999}1\", input", false, "1" + CappedPadding + "1")]
    [DataRow("string.Format(\"1{0,999999}1\", input)", false, "1" + CappedPadding + "1")]
    [DataRow("$\"1{value,999999}\"", false, "1" + CappedPadding + "1")]
    [DataRow("$\"{value,-999999}1\"", false, "1" + CappedPadding + "1")]
    public void Large_Alignments_Are_Capped(string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, "const string value = \"1\";");
    }

    [TestMethod]
    [DataRow("\"\"", true, "")]
    [DataRow("@\"123456789\"", true, "123456789")]
    [DataRow("@\"1234567890\"", false, "1234567890")]
    [DataRow("\"1234\\t5678\"", true, "1234\t5678")]
    [DataRow("\"1234\\t56789\"", false, "1234\t56789")]
    [DataRow("\"\\u003123456789\"", true, "123456789")]
    [DataRow("\"\\u0031234567890\"", false, "1234567890")]
    [DataRow("\"\"\"123456789\"\"\"", true, "123456789")]
    [DataRow("\"\"\"1234567890\"\"\"", false, "1234567890")]
    public void String_Literals_Use_Content_Length(string expression, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(expression, diagnosticExpected, expectedRead);
    }

    [TestMethod]
    [DataRow("\n    \"123456789\"", true, "123456789")]
    [DataRow("\n    \"1234567890\"", false, "1234567890")]
    [DataRow("\n\t\"123456789\"", true, "123456789")]
    [DataRow("\n\t\"1234567890\"", false, "1234567890")]
    [DataRow("\r\n    \"123456789\"", true, "123456789")]
    [DataRow("\r\n    \"1234567890\"", false, "1234567890")]
    [DataRow("\r\n\t\"123456789\"", true, "123456789")]
    [DataRow("\r\n\t\"1234567890\"", false, "1234567890")]
    [DataRow("\"1234\" +\n    \"56789\"", true, "123456789")]
    [DataRow("\"12345\" +\n    \"67890\"", false, "1234567890")]
    [DataRow("\"1234\" +\r\n\t\"56789\"", true, "123456789")]
    [DataRow("\"12345\" +\r\n\t\"67890\"", false, "1234567890")]
    [DataRow("\"123456789\" +\n    input", true, "123456789")]
    [DataRow("\"1234567890\" +\n    input", false, "1234567890")]
    [DataRow("\"123456789\" +\r\n\tinput", true, "123456789")]
    [DataRow("\"1234567890\" +\r\n\tinput", false, "1234567890")]
    [DataRow("$\"123456789{\n    input}\"", true, "123456789")]
    [DataRow("$\"1234567890{\n    input}\"", false, "1234567890")]
    [DataRow("$\"123456789{\r\n\tinput}\"", true, "123456789")]
    [DataRow("$\"1234567890{\r\n\tinput}\"", false, "1234567890")]
    [DataRow("\n    \"123456789{Value}\",\n    input", true, "123456789")]
    [DataRow("\n    \"1234567890{Value}\",\n    input", false, "1234567890")]
    [DataRow("\r\n\t\"123456789{Value}\",\r\n\tinput", true, "123456789")]
    [DataRow("\r\n\t\"1234567890{Value}\",\r\n\tinput", false, "1234567890")]
    [DataRow("string.Format(\n    \"123456789{0}\",\n    input)", true, "123456789")]
    [DataRow("string.Format(\n    \"1234567890{0}\",\n    input)", false, "1234567890")]
    [DataRow("string.Format(\r\n\t\"123456789{0}\",\r\n\tinput)", true, "123456789")]
    [DataRow("string.Format(\r\n\t\"1234567890{0}\",\r\n\tinput)", false, "1234567890")]
    public void Source_Line_Breaks_And_Indentation_Do_Not_Count_Towards_Message_Length(
        string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead);
    }

    [TestMethod]
    [DataRow("LogTrace", "\"Started\"", 0, true, "Started")]
    [DataRow("LogDebug", "\"Started\"", 0, true, "Started")]
    [DataRow("LogWarning", "\"Started\"", 0, true, "Started")]
    [DataRow("LogError", "\"Started\"", 0, true, "Started")]
    [DataRow("LogCritical", "\"Started\"", 0, true, "Started")]
    [DataRow("LogInformation", "new EventId(42, \"OrderProcessing\"), \"Started\"", 1, true, "Started")]
    [DataRow("LogError", "new System.Exception(\"A detailed exception message\"), \"Failed\"", 1, true, "Failed")]
    [DataRow("LogError", "new EventId(42), new System.Exception(), \"Failed\"", 2, true, "Failed")]
    [DataRow("LogError", "new System.Exception(), \"Order processing failed\"", 1, false, "Order processing failed")]
    [DataRow("LogError", "new System.Exception(), \"Bad {Id}\", \"12345\"", 1, true, "Bad 12345")]
    [DataRow("LogError", "new System.Exception(), \"Bad {Id}\", \"123456\"", 1, false, "Bad 123456")]
    [DataRow("Log", "LogLevel.Warning, \"Started\"", 1, true, "Started")]
    [DataRow("Log", "LogLevel.Error, new EventId(42), new System.Exception(), \"Failed\"", 3, true, "Failed")]
    [DataRow("LogInformation", "args: new object[] { \"12345\" }, message: \"Got {Id}\"", 1, true, "Got 12345")]
    [DataRow("LogError", "message: \"Failed\", exception: new System.Exception()", 0, true, "Failed")]
    public void Logging_Overloads_Use_Only_The_Rendered_Message_Length(
        string methodName, string arguments, int messageArgumentIndex, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead, methodName: methodName, messageArgumentIndex: messageArgumentIndex);
    }

    [TestMethod]
    [DataRow("\"Got {OrderIdentifier}\", \"12345\"", true, "Got 12345")]
    [DataRow("\"Got {OrderIdentifier}\", \"123456\"", false, "Got 123456")]
    [DataRow("\"{User} {Action}\", \"Sam\", \"login\"", true, "Sam login")]
    [DataRow("\"{User} {Action}\", \"Sam\", \"logout\"", false, "Sam logout")]
    [DataRow("\"{Value}{Value}\", \"1234\", \"56789\"", true, "123456789")]
    [DataRow("\"{Second}{First}\", \"12345\", \"67890\"", false, "1234567890")]
    [DataRow("\"Got {Id}\", new object[] { \"12345\" }", true, "Got 12345")]
    [DataRow("\"Got {Id}\", new object[] { \"123456\" }", false, "Got 123456")]
    [DataRow("\"Got {Id}\", [\"12345\"]", true, "Got 12345")]
    [DataRow("\"Got {Id}\", [\"123456\"]", false, "Got 123456")]
    [DataRow("\"Got {Id}\", \"\"", true, "Got")]
    [DataRow("\"{Id,-9}\", \"1\"", true, "1")]
    [DataRow("\"{Id,-10}\", \"1\"", true, "1")]
    [DataRow("\"{{Id}} {Id}\", \"1234\"", true, "{Id} 1234")]
    [DataRow("\"{{Id}} {Id}\", \"12345\"", false, "{Id} 12345")]
    [DataRow("\"{{OK}} {Id}\", 1", true, "{OK}")]
    [DataRow("\"{Value}\", \"{Id:D10}\"", true, "{Id:D10}")]
    public void Structured_Logging_Uses_Rendered_Values_In_Argument_Order(string arguments, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(arguments, diagnosticExpected, expectedRead);
    }

    [TestMethod]
    [DataRow("\"1234\" + \"56789\"", true, "123456789")]
    [DataRow("\"12345\" + \"67890\"", false, "1234567890")]
    [DataRow("string.Concat(\"Got \", \"12\", \"345\")", true, "Got 12345")]
    [DataRow("string.Concat(\"Got \", \"12\", \"3456\")", false, "Got 123456")]
    [DataRow("string.Concat(new string[] { \"Got \", \"12345\" })", true, "Got 12345")]
    [DataRow("string.Concat(new string[] { \"Got \", \"123456\" })", false, "Got 123456")]
    [DataRow("string.Concat(str1: \"12345\", str0: \"Got \" )", true, "Got 12345")]
    [DataRow("string.Format(\"{{{0}}}\", \"1234567\")", true, "{1234567}")]
    [DataRow("string.Format(\"{{{0}}}\", \"12345678\")", false, "{12345678}")]
    [DataRow("string.Format(\"{{X:D9}}{0}\", \"1\")", true, "{X:D9}1")]
    [DataRow("string.Format(\"Id:{0}\", 42)", true, "Id:")]
    public void Common_Formatting_Overloads_Use_Their_Result_Length(string expression, bool diagnosticExpected, string expectedRead)
    {
        VerifyMessage(expression, diagnosticExpected, expectedRead);
    }

    /// <summary>
    /// A long concatenation is rendered with a loop rather than recursion, so it can't overflow the stack.
    /// </summary>
    [TestMethod]
    [Timeout(30_000)]
    [DataRow(true, "123456789")]
    [DataRow(false, "1234567890")]
    public void Long_Concatenation_Is_Rendered(bool diagnosticExpected, string text)
    {
        var arguments = $"\"{text}\"" + string.Concat(Enumerable.Repeat(" + input", 10_000));

        VerifyMessage(arguments, diagnosticExpected, expectedRead: text);
    }

    /// <summary>
    /// Settings are read when the analyzer runs, and Roslyn reports any exception thrown by an analyzer as an AD0001 diagnostic.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MSTEST0046:Use 'Assert' instead of 'StringAssert'", Justification = "Assert gives a different result, and the test fails")]
    private void AssertAnalyzerThrowsInvalidConfig(string expectedMessage)
    {
        const string testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;
class ClassName
{
    private void Method(ILogger<ClassName> logger)
    {
        logger.LogInformation(""1234567890"");
    }
}";
        var diagnostics = GetSortedDiagnosticsFromDocuments(
            GetCSharpDiagnosticAnalyzer(), [CreateDocument(testCode)]);

        var analyzerException = diagnostics.SingleOrDefault(d => d.Id == "AD0001");
        Assert.IsNotNull(analyzerException, "Expected the analyzer to throw an exception.");
        StringAssert.Contains(analyzerException.GetMessage(), nameof(InvalidConfigException));
        StringAssert.Contains(analyzerException.GetMessage(), expectedMessage);
    }

    /// <summary>
    /// <paramref name="flaggedArgument"/> is where the diagnostic is reported, if the message is too short.
    /// <paramref name="expectedRead"/> is <see langword="null"/> when there's no message to read.
    /// </summary>
    [TestMethod]
    [DataRow("EventId = 1, Level = LogLevel.Information, Message = \"123456789\"", "Message = \"123456789\"", true, "123456789")]
    [DataRow("EventId = 1, Level = LogLevel.Information, Message = \"1234567890\"", null, false, "1234567890")]
    [DataRow("1, LogLevel.Information, \"123456789\"", "\"123456789\"", true, "123456789")]
    [DataRow("1, LogLevel.Information, \"1234567890\"", null, false, "1234567890")]
    [DataRow("LogLevel.Information, \"123456789\"", "\"123456789\"", true, "123456789")]
    [DataRow("\"123456789\"", "\"123456789\"", true, "123456789")]
    [DataRow("message: \"123456789\", level: LogLevel.Information, eventId: 1", "message: \"123456789\"", true, "123456789")]
    [DataRow("Level = LogLevel.Information, Message = \"   12345678   \"", "Message = \"   12345678   \"", true, "12345678")]
    [DataRow("Level = LogLevel.Information, Message = ShortMessage", "Message = ShortMessage", true, "12345678")]
    [DataRow("Level = LogLevel.Information, Message = \"{OrderId}\"", "Message = \"{OrderId}\"", true, "")]
    [DataRow("Level = LogLevel.Information, Message = LongMessage", null, false, "1234567890")]
    [DataRow("Level = LogLevel.Information, Message = \"Processed {OrderId}\"", "Message = \"Processed {OrderId}\"", true, "Processed")]
    [DataRow("Level = LogLevel.Information, Message = \"Processed order {OrderId}\"", null, false, "Processed order")]
    [DataRow("EventId = 1, Level = LogLevel.Information", null, false, null)]
    [DataRow("LogLevel.Information", null, false, null)]
    public void LoggerMessage_Attribute_Message_Is_Checked(string attributeArguments, string flaggedArgument, bool diagnosticExpected, string expectedRead)
    {
        var testCode = $$"""
            using Microsoft.Extensions.Logging;
            namespace ConsoleApplication;
            static partial class Log
            {
                private const string ShortMessage = "12345678";
                private const string LongMessage = "1234567890";

                [LoggerMessage({{attributeArguments}})]
                static partial void Processed(ILogger logger);

                static void Main(string[] args)
                {
                }
            }
            """;

        VerifyCodeAt(testCode, flaggedArgument, diagnosticExpected, expectedRead);
    }

    [TestMethod]
    public void LoggerMessage_Attribute_Uses_Configured_Minimum_Length()
    {
        ConfigureMinimumLength("5");
        const string testCode = """
            using Microsoft.Extensions.Logging;
            namespace ConsoleApplication;
            static partial class Log
            {
                [LoggerMessage(Level = LogLevel.Information, Message = "1234")]
                static partial void Short(ILogger logger);

                [LoggerMessage(Level = LogLevel.Information, Message = "12345")]
                static partial void LongEnough(ILogger logger);

                static void Main(string[] args)
                {
                }
            }
            """;

        VerifyDiagnostic(testCode, BuildExpectedResultAt(testCode, "Message = \"1234\"", minLength: 5));
        AssertMessagesRead("1234", "12345");
    }

    /// <summary>
    /// <paramref name="flaggedArgument"/> is where the diagnostic is reported, if the message is too short.
    /// <paramref name="expectedRead"/> is <see langword="null"/> when there's no message to read.
    /// </summary>
    [TestMethod]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"123456789\")", "\"123456789\"", true, "123456789")]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"1234567890\")", null, false, "1234567890")]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"{Id}\")", "\"{Id}\"", true, "")]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"Processed {Id}\")", "\"Processed {Id}\"", true, "Processed")]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"Processed order {Id}\")", null, false, "Processed order")]
    [DataRow("LoggerMessage.Define(formatString: \"123456789\", eventId: new EventId(1), logLevel: LogLevel.Information)", "formatString: \"123456789\"", true, "123456789")]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), input)", "input", true, "")]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"12\" + input)", "\"12\" + input", true, "12")]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"1234567890\" + input)", null, false, "1234567890")]
    [DataRow("LoggerMessage.DefineScope(\"123456789\")", null, false, null)]
    public void LoggerMessage_Define_Format_String_Is_Checked(string expression, string flaggedArgument, bool diagnosticExpected, string expectedRead)
    {
        var testCode = $$"""
            using Microsoft.Extensions.Logging;
            namespace ConsoleApplication;
            class ClassName
            {
                private object Method(string input)
                {
                    return {{expression}};
                }

                static void Main(string[] args)
                {
                }
            }
            """;

        VerifyCodeAt(testCode, flaggedArgument, diagnosticExpected, expectedRead);
    }

    /// <summary>
    /// Verifies the analyzer reads <paramref name="expectedRead"/> (or nothing, if it's <see langword="null"/>), and reports a
    /// diagnostic at the last occurrence of <paramref name="flaggedArgument"/> if <paramref name="diagnosticExpected"/>.
    /// </summary>
    private void VerifyCodeAt(string testCode, string flaggedArgument, bool diagnosticExpected, string expectedRead,
        int minLength = DefaultMinLength)
    {
        AssertTestDataIsConsistent(diagnosticExpected, expectedRead, minLength);
        string[] expectedReads = expectedRead == null ? [] : [expectedRead];

        if (!diagnosticExpected)
        {
            Assert.IsNull(flaggedArgument, "No diagnostic is expected, so no argument should be flagged.");
            VerifyAndAssertMessagesRead(() => VerifyNoDiagnostic(testCode), expectedReads);
            return;
        }

        var expected = BuildExpectedResultAt(testCode, flaggedArgument, minLength);
        VerifyAndAssertMessagesRead(() => VerifyDiagnostic(testCode, expected), expectedReads);
    }

    /// <summary>
    /// Checks a test's data doesn't contradict itself: a diagnostic should be expected if, and only if, the message expected
    /// to be read is shorter than the minimum length.
    /// </summary>
    private static void AssertTestDataIsConsistent(bool diagnosticExpected, string expectedRead, int minLength)
    {
        var tooShort = expectedRead != null && expectedRead.Length < minLength;

        Assert.AreEqual(tooShort, diagnosticExpected,
            $"Invalid test data: \"{expectedRead}\" has length {expectedRead?.Length}, and the minimum length is {minLength}, " +
            $"so diagnosticExpected should be {tooShort.ToString().ToLowerInvariant()}.");
    }

    /// <summary>
    /// Builds the expected diagnostic at the last occurrence of <paramref name="flaggedArgument"/>.
    /// </summary>
    private static DiagnosticResult BuildExpectedResultAt(string testCode, string flaggedArgument, int minLength)
    {
        Assert.IsNotNull(flaggedArgument, "The message is too short, so the argument that should be flagged must be given.");

        var index = testCode.LastIndexOf(flaggedArgument, System.StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"'{flaggedArgument}' was not found in the test code.");

        var line = testCode.Take(index).Count(character => character == '\n') + 1;
        var column = index - testCode.LastIndexOf('\n', index);

        return BuildExpectedResult(line, column, minLength);
    }

    private void ConfigureMinimumLength(string setting)
    {
        var key = new SettingsKey(LogMessagesMinimumLengthAnalyzer.Id,
            LogMessagesMinimumLengthAnalyzer.LogMessageMinimumLengthSetting);
        var options = Substitute.For<AnalyzerConfigOptions>();
        options.TryGetValue(key.ToString(), out Arg.Any<string>())
            .Returns(call =>
            {
                call[1] = setting;
                return true;
            });
        var provider = Substitute.For<AnalyzerConfigOptionsProvider>();
        provider.GetOptions(Arg.Any<SyntaxTree>()).Returns(options);

        _settingsReader = new EditorConfigSettingsReader(
            new AnalyzerOptions([], provider));
    }

    /// <summary>
    /// Verifies the analyzer reads the message passed to the logger as <paramref name="expectedRead"/>, and reports a
    /// diagnostic if <paramref name="diagnosticExpected"/>.
    /// </summary>
    private void VerifyMessage(string arguments, bool diagnosticExpected, string expectedRead, string declaration = "",
        int minLength = DefaultMinLength, string methodName = "LogInformation", int messageArgumentIndex = 0)
    {
        AssertTestDataIsConsistent(diagnosticExpected, expectedRead, minLength);

        var testCode = $$"""
            using Microsoft.Extensions.Logging;
            namespace ConsoleApplication;
            class ClassName
            {
                private void Method(ILogger<ClassName> logger, string input)
                {
                    {{declaration}}
                    logger.{{methodName}}({{arguments}});
                }

                static void Main(string[] args)
                {
                }
            }

            static class Messages
            {
                public const string Short = "123456789";
                public const string Long = "1234567890";
                public const string Template = "Order {Id}";
                public const string LongTemplate = "Order processed {Id}";
            }
            """;

        if (diagnosticExpected)
        {
            var messageArgument = CSharpSyntaxTree.ParseText(testCode).GetRoot()
                .DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Single(invocation => invocation.Expression.ToString() == $"logger.{methodName}")
                .ArgumentList.Arguments[messageArgumentIndex];
            var position = messageArgument.GetLocation().GetLineSpan().StartLinePosition;
            var expected = BuildExpectedResult(position.Line + 1, position.Character + 1, minLength);

            VerifyAndAssertMessagesRead(() => VerifyDiagnostic(testCode, expected), expectedRead);
        }
        else
        {
            VerifyAndAssertMessagesRead(() => VerifyNoDiagnostic(testCode), expectedRead);
        }
    }

    /// <summary>
    /// Runs <paramref name="verify"/>, then checks the messages read. If <paramref name="verify"/> fails, the messages read
    /// are checked first, as a message read differently than expected is the more useful failure.
    /// </summary>
    private void VerifyAndAssertMessagesRead(System.Action verify, params string[] expectedReads)
    {
        try
        {
            verify();
        }
        catch
        {
            AssertMessagesRead(expectedReads);
            throw;
        }

        AssertMessagesRead(expectedReads);
    }

    /// <summary>
    /// Asserts the analyzer read exactly <paramref name="expectedReads"/>, in any order, as each is measured (i.e. trimmed).
    /// </summary>
    private void AssertMessagesRead(params string[] expectedReads)
    {
        static string Describe(System.Collections.Generic.IEnumerable<string> messages) => string.Join(", ",
            messages.OrderBy(message => message, System.StringComparer.Ordinal)
                .Select(message => $"\"{message}\" (length {message.Length})"));

        var expected = Describe(expectedReads);
        var actual = Describe(_messagesRead.Values);

        Assert.AreEqual(expected, actual, "The analyzer didn't read the messages expected.");
    }
}

