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

[TestClass]
public class LogMessagesMinimumLengthAnalyzerTests : CodeFixVerifier
{
    private ISettingsReader _settingsReader;

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
        return _settingsReader == null
            ? new LogMessagesMinimumLengthAnalyzer()
            : new LogMessagesMinimumLengthAnalyzer(_settingsReader);
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
    }

    [TestMethod]
    [DataRow("string.Format(\"{0}\", \"123456789\")", true)]
    [DataRow("string.Format(\"{0}{1}\", \"1234\", \"56789\")", true)]
    [DataRow("string.Format(\"{0}{1}{2}\", \"123\", \"456\", \"789\")", true)]
    [DataRow("string.Format(\"{0}{1}{2}{3}\", \"12\", \"34\", \"56\", \"789\")", true)]
    [DataRow("string.Format(\"{0}{1}{2}{3}\", \"12\", \"34\", \"56\", \"7890\")", false)]
    [DataRow("string.Format(arg1: \"\", format: \"{0}{0}{1}\", arg0: \"12345\")", false)]
    [DataRow("string.Format(\"{0}{1}\", new object[] { \"1234\", \"56789\" })", true)]
    public void String_Format_Uses_Constant_String_Arguments(string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("20", "1234567890123456789", true)]
    [DataRow("20", "12345678901234567890", false)]
    [DataRow("20", "123456789012345678901", false)]
    [DataRow("5", "1234", true)]
    [DataRow("5", "12345", false)]
    [DataRow("5", "123456", false)]
    [DataRow("1", "", true)]
    [DataRow("1", "1", false)]
    [DataRow("100", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [DataRow("100", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public void Configured_Minimum_Length_Is_Used(string setting, string message, bool diagnosticExpected)
    {
        ConfigureMinimumLength(setting);

        VerifyMessage($"\"{message}\"", diagnosticExpected, minLength: int.Parse(setting));
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
    [DataRow("Messages.Short", "", true)]
    [DataRow("Messages.Long", "", false)]
    [DataRow("value", "const string value = \"123456789\";", true)]
    [DataRow("value", "const string value = \"1234567890\";", false)]
    [DataRow("\n    value", "const string value =\n    \"1234567890\";", false)]
    [DataRow("\r\n\tvalue", "const string value =\r\n\t\"1234567890\";", false)]
    [DataRow("$\"{Messages.Short}\"", "", true)]
    [DataRow("$\"{Messages.Long}\"", "", false)]
    [DataRow("$\"12345678{value}\"", "const string value = \"1\";", true)]
    [DataRow("$\"123456789{value}\"", "const string value = \"1\";", false)]
    [DataRow("\"{Value}\", Messages.Short", "", true)]
    [DataRow("\"{Value}\", Messages.Long", "", false)]
    [DataRow("value + \"6789\"", "const string value = \"12345\";", true)]
    [DataRow("value + \"67890\"", "const string value = \"12345\";", false)]
    [DataRow("string.Concat(Messages.Short, \"\")", "", true)]
    [DataRow("string.Concat(Messages.Short, \"0\")", "", false)]
    [DataRow("string.Format(\"{0}\", Messages.Short)", "", true)]
    [DataRow("string.Format(\"{0}\", Messages.Long)", "", false)]
    [DataRow("string.Format(format, \"123456789\")", "const string format = \"{0}\";", true)]
    [DataRow("string.Format(format, \"1234567890\")", "const string format = \"{0}\";", false)]
    [DataRow("Messages.Template, input", "", true)] // "Order {Id}" with a nominal value is "Order"
    [DataRow("Messages.LongTemplate, input", "", false)]
    public void Constant_Strings_Are_Read(string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    /// <summary>
    /// Anything that isn't a compile-time constant string is given the nominal length of zero
    /// (<see cref="LogMessagesMinimumLengthAnalyzer.NominalParameterLength"/>), whether it's the whole message or only
    /// part of it, as the length of its name or source text says nothing about the length of the value that will be logged.
    /// </summary>
    [TestMethod]
    // The whole message
    [DataRow("input", "", true)]
    [DataRow("input.Trim()", "", true)]
    [DataRow("value", "var value = \"1234567890\";", true)]
    // Interpolated strings
    [DataRow("$\"{input}\"", "", true)]
    [DataRow("$\"123456789{input}\"", "", true)]
    [DataRow("$\"1234567890{input}\"", "", false)]
    [DataRow("$\"123456789{input:N}\"", "", true)]
    [DataRow("$\"{value}\"", "var value = \"1234567890\";", true)]
    // Concatenation
    [DataRow("\"123456789\" + input", "", true)]
    [DataRow("\"1234567890\" + input", "", false)]
    [DataRow("input + \"123456789\"", "", true)]
    [DataRow("input + \"1234567890\"", "", false)]
    [DataRow("input + input", "", true)]
    [DataRow("\"12345\" + value", "var value = \"67890\";", true)]
    // string.Concat, string.Join and string.Format
    [DataRow("string.Concat(\"123456789\", input)", "", true)]
    [DataRow("string.Concat(\"1234567890\", input)", "", false)]
    [DataRow("string.Join(\", \", \"12345678\", input)", "", true)]
    [DataRow("string.Join(\", \", \"123456789\", input)", "", false)]
    [DataRow("string.Join(\", \", values)", "var values = new[] { \"1234567890\" };", true)]
    [DataRow("string.Format(\"123456789{0}\", input)", "", true)]
    [DataRow("string.Format(\"1234567890{0}\", input)", "", false)]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"123456789{0}\", input)", "", true)]
    [DataRow("string.Format(format, \"1234567890\")", "var format = \"{0}\";", true)]
    // Format items that can't be matched to an argument are given the nominal length, but the format string is still read
    [DataRow("string.Format(\"{1}\", \"1234567890\")", "", true)]
    [DataRow("string.Format(\"123456789{1}\", \"1234567890\")", "", true)]
    [DataRow("string.Format(\"1234567890{1}\", input)", "", false)]
    [DataRow("string.Format(\"Order processing failed: {1}\", input)", "", false)]
    [DataRow("string.Format(\"123456789{x}\", input)", "", true)]
    [DataRow("string.Format(\"1234567890{x}\", input)", "", false)]
    [DataRow("string.Format(\"1{x,8}1\", input)", "", false)]
    [DataRow("string.Format(\"12345678{\", input)", "", true)] // An unmatched brace is measured as written
    [DataRow("string.Format(\"123456789{\", input)", "", false)]
    [DataRow("string.Format(\"123456789{0}\", values)", "var values = new object[] { \"1234567890\" };", true)]
    [DataRow("string.Format(\"1234567890{0}\", values)", "var values = new object[] { input };", false)]
    [DataRow("string.Format(\"Order processing failed: {0}\", values)", "var values = new object[] { input };", false)]
    [DataRow("string.Format(\"123456789{1}\", values)", "var values = new object[] { input };", true)]
    [DataRow("string.Format(\"1{0,8}1\", values)", "var values = new object[] { input };", false)]
    // Message template values
    [DataRow("\"123456789{Value}\", input", "", true)]
    [DataRow("\"1234567890{Value}\", input", "", false)]
    [DataRow("\"{Value}\", value", "var value = \"1234567890\";", true)]
    [DataRow("\"{Known} {Unknown}\", \"12345678\", input", "", true)]
    [DataRow("\"{Known} {Unknown}\", \"1234567890\", input", "", false)]
    [DataRow("\"123456789{Id}\", values", "var values = new object[] { \"1\" };", true)]
    public void Values_That_Are_Not_Constant_Strings_Are_Given_The_Nominal_Length(
        string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    /// <summary>
    /// Only constant strings are read, so constants of other types are given the nominal length, e.g. "Order {Id}" with
    /// a value of 1234567890 is measured as "Order". Format specifiers have no effect, and null isn't shown as "(null)".
    /// </summary>
    [TestMethod]
    [DataRow("(string)null")]
    [DataRow("\"Got {Id}\", (object)null")]
    [DataRow("\"Got {Id}\", new object[] { null }")]
    [DataRow("$\"Got {(object)null}\"")]
    [DataRow("string.Format(\"Got {0}\", (object)null)")]
    [DataRow("\"Order {Id}\", 1234567890")]
    [DataRow("\"Done {Success}\", false")]
    [DataRow("$\"Order {1234567890}\"")]
    [DataRow("string.Format(\"Order {0}\", 1234567890)")]
    [DataRow("\"Order {Id:D10}\", 42")]
    [DataRow("$\"Order {42:D10}\"")]
    [DataRow("string.Format(\"Order {0:D10}\", 42)")]
    [DataRow("\"{{literal}} {Id:D9}\", 42")]
    public void Constants_That_Are_Not_Strings_Are_Given_The_Nominal_Length(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: true);
    }

    /// <summary>
    /// Without values, the logger doesn't format a template, so its placeholders are logged as written.
    /// </summary>
    [TestMethod]
    [DataRow("\"{UserId}\"", true)]
    [DataRow("\"Id {UserId}\"", false)]
    public void Template_Without_Values_Is_Measured_As_Written(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    /// <summary>
    /// An alignment pads a value, whether it's a constant string ("1" here) or given the nominal length.
    /// Padding outside the message is trimmed, but padding between other text counts towards its length.
    /// </summary>
    [TestMethod]
    [DataRow("$\"{value,9}\"", true)] // "        1"
    [DataRow("$\"{value,-10}\"", true)]
    [DataRow("\"{Value,9}\", value", true)]
    [DataRow("\"{{{Value}}}\", value", true)]
    [DataRow("$\"1234567{value,2}\"", true)]
    [DataRow("$\"12345678{value,2}\"", false)]
    [DataRow("$\"{value,-2}1234567\"", true)] // "1 1234567"
    [DataRow("$\"{value,-2}12345678\"", false)]
    [DataRow("\"12345678{Value,2}\", value", false)]
    [DataRow("\"{Value,-2}12345678\", value", false)]
    [DataRow("string.Format(\"12345678{0,2}\", value)", false)]
    [DataRow("$\"1{input,8}\"", true)]
    [DataRow("$\"1{input,7}1\"", true)]
    [DataRow("$\"1{input,8}1\"", false)]
    [DataRow("$\"1{input,-8}1\"", false)]
    [DataRow("\"1{Value,8}1\", input", false)]
    [DataRow("\"1{Value,8:D2}1\", input", false)]
    [DataRow("string.Format(\"1{0,8}1\", input)", false)]
    public void Alignment_Pads_Values(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, "const string value = \"1\";");
    }

    /// <summary>
    /// Large alignments are capped, without changing whether the message is long enough.
    /// </summary>
    [TestMethod]
    [DataRow("$\"{input,999999}\"", true)]
    [DataRow("$\"{input,-999999}\"", true)]
    [DataRow("$\"1{input,999999}1\"", false)]
    [DataRow("\"1{Value,999999}1\", input", false)]
    [DataRow("string.Format(\"1{0,999999}1\", input)", false)]
    [DataRow("$\"1{value,999999}\"", false)]
    [DataRow("$\"{value,-999999}1\"", false)]
    public void Large_Alignments_Are_Capped(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, "const string value = \"1\";");
    }

    [TestMethod]
    [DataRow("\"\"", true)]
    [DataRow("@\"123456789\"", true)]
    [DataRow("@\"1234567890\"", false)]
    [DataRow("\"1234\\t5678\"", true)]
    [DataRow("\"1234\\t56789\"", false)]
    [DataRow("\"\\u003123456789\"", true)]
    [DataRow("\"\\u0031234567890\"", false)]
    [DataRow("\"\"\"123456789\"\"\"", true)]
    [DataRow("\"\"\"1234567890\"\"\"", false)]
    public void String_Literals_Use_Content_Length(string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\n    \"123456789\"", true)]
    [DataRow("\n    \"1234567890\"", false)]
    [DataRow("\n\t\"123456789\"", true)]
    [DataRow("\n\t\"1234567890\"", false)]
    [DataRow("\r\n    \"123456789\"", true)]
    [DataRow("\r\n    \"1234567890\"", false)]
    [DataRow("\r\n\t\"123456789\"", true)]
    [DataRow("\r\n\t\"1234567890\"", false)]
    [DataRow("\"1234\" +\n    \"56789\"", true)]
    [DataRow("\"12345\" +\n    \"67890\"", false)]
    [DataRow("\"1234\" +\r\n\t\"56789\"", true)]
    [DataRow("\"12345\" +\r\n\t\"67890\"", false)]
    [DataRow("\"123456789\" +\n    input", true)]
    [DataRow("\"1234567890\" +\n    input", false)]
    [DataRow("\"123456789\" +\r\n\tinput", true)]
    [DataRow("\"1234567890\" +\r\n\tinput", false)]
    [DataRow("$\"123456789{\n    input}\"", true)]
    [DataRow("$\"1234567890{\n    input}\"", false)]
    [DataRow("$\"123456789{\r\n\tinput}\"", true)]
    [DataRow("$\"1234567890{\r\n\tinput}\"", false)]
    [DataRow("\n    \"123456789{Value}\",\n    input", true)]
    [DataRow("\n    \"1234567890{Value}\",\n    input", false)]
    [DataRow("\r\n\t\"123456789{Value}\",\r\n\tinput", true)]
    [DataRow("\r\n\t\"1234567890{Value}\",\r\n\tinput", false)]
    [DataRow("string.Format(\n    \"123456789{0}\",\n    input)", true)]
    [DataRow("string.Format(\n    \"1234567890{0}\",\n    input)", false)]
    [DataRow("string.Format(\r\n\t\"123456789{0}\",\r\n\tinput)", true)]
    [DataRow("string.Format(\r\n\t\"1234567890{0}\",\r\n\tinput)", false)]
    public void Source_Line_Breaks_And_Indentation_Do_Not_Count_Towards_Message_Length(
        string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("LogTrace", "\"Started\"", 0, true)]
    [DataRow("LogDebug", "\"Started\"", 0, true)]
    [DataRow("LogWarning", "\"Started\"", 0, true)]
    [DataRow("LogError", "\"Started\"", 0, true)]
    [DataRow("LogCritical", "\"Started\"", 0, true)]
    [DataRow("LogInformation", "new EventId(42, \"OrderProcessing\"), \"Started\"", 1, true)]
    [DataRow("LogError", "new System.Exception(\"A detailed exception message\"), \"Failed\"", 1, true)]
    [DataRow("LogError", "new EventId(42), new System.Exception(), \"Failed\"", 2, true)]
    [DataRow("LogError", "new System.Exception(), \"Order processing failed\"", 1, false)]
    [DataRow("LogError", "new System.Exception(), \"Bad {Id}\", \"12345\"", 1, true)]
    [DataRow("LogError", "new System.Exception(), \"Bad {Id}\", \"123456\"", 1, false)]
    [DataRow("Log", "LogLevel.Warning, \"Started\"", 1, true)]
    [DataRow("Log", "LogLevel.Error, new EventId(42), new System.Exception(), \"Failed\"", 3, true)]
    [DataRow("LogInformation", "args: new object[] { \"12345\" }, message: \"Got {Id}\"", 1, true)]
    [DataRow("LogError", "message: \"Failed\", exception: new System.Exception()", 0, true)]
    public void Logging_Overloads_Use_Only_The_Rendered_Message_Length(
        string methodName, string arguments, int messageArgumentIndex, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, methodName: methodName, messageArgumentIndex: messageArgumentIndex);
    }

    [TestMethod]
    [DataRow("\"Got {OrderIdentifier}\", \"12345\"", true)]
    [DataRow("\"Got {OrderIdentifier}\", \"123456\"", false)]
    [DataRow("\"{User} {Action}\", \"Sam\", \"login\"", true)]
    [DataRow("\"{User} {Action}\", \"Sam\", \"logout\"", false)]
    [DataRow("\"{Value}{Value}\", \"1234\", \"56789\"", true)]
    [DataRow("\"{Second}{First}\", \"12345\", \"67890\"", false)]
    [DataRow("\"Got {Id}\", new object[] { \"12345\" }", true)]
    [DataRow("\"Got {Id}\", new object[] { \"123456\" }", false)]
    [DataRow("\"Got {Id}\", \"\"", true)]
    [DataRow("\"{Id,-9}\", \"1\"", true)]
    [DataRow("\"{Id,-10}\", \"1\"", true)]
    [DataRow("\"{{Id}} {Id}\", \"1234\"", true)]
    [DataRow("\"{{Id}} {Id}\", \"12345\"", false)]
    [DataRow("\"{{OK}} {Id}\", 1", true)]
    [DataRow("\"{Value}\", \"{Id:D10}\"", true)]
    public void Structured_Logging_Uses_Rendered_Values_In_Argument_Order(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\"1234\" + \"56789\"", true)]
    [DataRow("\"12345\" + \"67890\"", false)]
    [DataRow("string.Concat(\"Got \", \"12\", \"345\")", true)]
    [DataRow("string.Concat(\"Got \", \"12\", \"3456\")", false)]
    [DataRow("string.Concat(new string[] { \"Got \", \"12345\" })", true)]
    [DataRow("string.Concat(new string[] { \"Got \", \"123456\" })", false)]
    [DataRow("string.Concat(str1: \"12345\", str0: \"Got \" )", true)]
    [DataRow("string.Format(\"{{{0}}}\", \"1234567\")", true)]
    [DataRow("string.Format(\"{{{0}}}\", \"12345678\")", false)]
    [DataRow("string.Format(\"{{X:D9}}{0}\", \"1\")", true)]
    [DataRow("string.Format(\"Id:{0}\", 42)", true)]
    public void Common_Formatting_Overloads_Use_Their_Result_Length(string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    /// <summary>
    /// A long concatenation is rendered with a loop rather than recursion, so it can't overflow the stack.
    /// </summary>
    [TestMethod]
    [Timeout(30_000)]
    [DataRow("\"123456789\"", true)]
    [DataRow("\"1234567890\"", false)]
    public void Long_Concatenation_Is_Rendered(string text, bool diagnosticExpected)
    {
        var arguments = text + string.Concat(Enumerable.Repeat(" + input", 10_000));

        VerifyMessage(arguments, diagnosticExpected);
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

    [TestMethod]
    [DataRow("EventId = 1, Level = LogLevel.Information, Message = \"123456789\"", "Message = \"123456789\"", true)]
    [DataRow("EventId = 1, Level = LogLevel.Information, Message = \"1234567890\"", null, false)]
    [DataRow("1, LogLevel.Information, \"123456789\"", "\"123456789\"", true)]
    [DataRow("1, LogLevel.Information, \"1234567890\"", null, false)]
    [DataRow("LogLevel.Information, \"123456789\"", "\"123456789\"", true)]
    [DataRow("\"123456789\"", "\"123456789\"", true)]
    [DataRow("message: \"123456789\", level: LogLevel.Information, eventId: 1", "message: \"123456789\"", true)]
    [DataRow("Level = LogLevel.Information, Message = \"   12345678   \"", "Message = \"   12345678   \"", true)]
    [DataRow("Level = LogLevel.Information, Message = ShortMessage", "Message = ShortMessage", true)]
    [DataRow("Level = LogLevel.Information, Message = \"{OrderId}\"", "Message = \"{OrderId}\"", true)]
    [DataRow("Level = LogLevel.Information, Message = LongMessage", null, false)]
    [DataRow("Level = LogLevel.Information, Message = \"Processed {OrderId}\"", "Message = \"Processed {OrderId}\"", true)]
    [DataRow("Level = LogLevel.Information, Message = \"Processed order {OrderId}\"", null, false)]
    [DataRow("EventId = 1, Level = LogLevel.Information", null, false)]
    [DataRow("LogLevel.Information", null, false)]
    public void LoggerMessage_Attribute_Message_Is_Checked(string attributeArguments, string flaggedArgument, bool diagnosticExpected)
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

        VerifyCodeAt(testCode, flaggedArgument, diagnosticExpected);
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

        VerifyCodeAt(testCode, "Message = \"1234\"", diagnosticExpected: true, minLength: 5);
    }

    [TestMethod]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"123456789\")", "\"123456789\"", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"1234567890\")", null, false)]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"{Id}\")", "\"{Id}\"", true)]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"Processed {Id}\")", "\"Processed {Id}\"", true)]
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"Processed order {Id}\")", null, false)]
    [DataRow("LoggerMessage.Define(formatString: \"123456789\", eventId: new EventId(1), logLevel: LogLevel.Information)", "formatString: \"123456789\"", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), input)", "input", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"12\" + input)", "\"12\" + input", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"1234567890\" + input)", null, false)]
    [DataRow("LoggerMessage.DefineScope(\"123456789\")", null, false)]
    public void LoggerMessage_Define_Format_String_Is_Checked(string expression, string flaggedArgument, bool diagnosticExpected)
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

        VerifyCodeAt(testCode, flaggedArgument, diagnosticExpected);
    }

    /// <summary>
    /// Verifies a diagnostic is reported at the last occurrence of <paramref name="flaggedArgument"/>, or that none is reported.
    /// </summary>
    private void VerifyCodeAt(string testCode, string flaggedArgument, bool diagnosticExpected, int minLength = 10)
    {
        if (!diagnosticExpected)
        {
            VerifyNoDiagnostic(testCode);
            return;
        }

        var index = testCode.LastIndexOf(flaggedArgument, System.StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"'{flaggedArgument}' was not found in the test code.");

        var line = testCode.Take(index).Count(character => character == '\n') + 1;
        var column = index - testCode.LastIndexOf('\n', index) ;

        VerifyDiagnostic(testCode, BuildExpectedResult(line, column, minLength));
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

    private void VerifyMessage(string arguments, bool diagnosticExpected, string declaration = "", int minLength = 10,
        string methodName = "LogInformation", int messageArgumentIndex = 0)
    {
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

            VerifyDiagnostic(testCode, BuildExpectedResult(position.Line + 1, position.Character + 1, minLength));
        }
        else
        {
            VerifyNoDiagnostic(testCode);
        }
    }
}

