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
            Message = $"Log messages should be at least {minLength} characters long",
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
    [DataRow("string.Format(\"{0}{1}{2}{3}\", \"123\", null, \"456\", \"789\")", true)]
    [DataRow("string.Format(\"{0}\", input)", false)]
    [DataRow("string.Format(\"{1}\", \"123456789\")", false)]
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
    public void Minimum_Length_Greater_Than_Fifty_Throws_When_Configuration_Is_Read()
    {
        ConfigureMinimumLength("51");
        var key = new SettingsKey(LogMessagesMinimumLengthAnalyzer.Id,
            LogMessagesMinimumLengthAnalyzer.LogMessageMinimumLengthSetting);

        AssertAnalyzerThrowsInvalidConfig(
            $"Value for '{key.ToString().ToLowerInvariant()}' in '.editorconfig' must be at most 50.");
    }

    [TestMethod]
    [DataRow("var value = \"1\";", "$\"{value}\"", true)]
    [DataRow("var value = \"123456789\";", "$\"{value}\"", true)]
    [DataRow("var value = \"1234567890\";", "$\"{value}\"", false)]
    [DataRow("var value = \"1\";", "$\"12345678{value}\"", true)]
    [DataRow("var value = \"1\";", "$\"123456789{value}\"", false)]
    public void Interpolated_Local_Variable_Uses_Known_Value_Length(
        string declaration, string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected, declaration);
    }

    [TestMethod]
    [DataRow("$\"{input}\"", false)]
    [DataRow("$\"12345678{input}\"", false)]
    [DataRow("$\"123456789{input}\"", false)]
    [DataRow("$\"1234567{input}{input}\"", false)]
    [DataRow("$\"12345678{input}{input}\"", false)]
    public void No_Diagnostic_For_Interpolated_Unresolved_Method_Parameter(
        string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("var value = \"1\";", "\"{Value}\", value", true)]
    [DataRow("var value = \"123456789\";", "\"{Value}\", value", true)]
    [DataRow("var value = \"1234567890\";", "\"{Value}\", value", false)]
    [DataRow("var value = \"1\";", "\"12345678{Value}\", value", true)]
    [DataRow("var value = \"1\";", "\"123456789{Value}\", value", false)]
    public void Message_Template_With_Local_Variable_Uses_Known_Value_Length(
        string declaration, string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    [TestMethod]
    [DataRow("\"{Value}\", input", false)]
    [DataRow("\"12345678{Value}\", input", false)]
    [DataRow("\"123456789{Value}\", input", false)]
    [DataRow("\"1234567{First}{Second}\", input, input", false)]
    [DataRow("\"12345678{First}{Second}\", input, input", false)]
    public void No_Diagnostic_For_Message_Template_With_Unresolved_Method_Parameter(
        string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("var value = \"1\";", "\"12345678\" + value", true)]
    [DataRow("var value = \"1\";", "\"123456789\" + value", false)]
    [DataRow("var value = \"1234567890\";", "\"\" + value", false)]
    [DataRow("var value = \"12345\";", "value + \"6789\"", true)]
    [DataRow("var value = \"12345\";", "value + \"67890\"", false)]
    public void Concatenated_Local_Variable_Uses_Known_Value_Length(
        string declaration, string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected, declaration);
    }

    [TestMethod]
    [DataRow("\"12345678\" + input", false)]
    [DataRow("\"123456789\" + input", false)]
    [DataRow("input + \"12345678\"", false)]
    [DataRow("input + \"123456789\"", false)]
    [DataRow("\"1234567\" + input + input", false)]
    [DataRow("\"12345678\" + input + input", false)]
    public void No_Diagnostic_For_Concatenated_Unresolved_Method_Parameter(
        string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\"1234\" + \"56789\"", true)]
    [DataRow("\"12345\" + \"67890\"", false)]
    [DataRow("string.Format(\"12345678{0}\", input)", false)]
    [DataRow("string.Format(\"123456789{0}\", input)", false)]
    [DataRow("string.Format(\"{0}\", \"123456789\")", true)]
    [DataRow("string.Format(\"{0}\", \"1234567890\")", false)]
    [DataRow("string.Concat(\"12345678\", input)", false)]
    [DataRow("string.Concat(\"123456789\", input)", false)]
    public void Common_String_Construction_Uses_Message_Length(string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
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
    [DataRow("const string value = \"123456789\";", true)]
    [DataRow("const string value = \"1234567890\";", false)]
    [DataRow("var value = \"123456789\";", true)]
    [DataRow("var value = \"1234567890\";", false)]
    public void Message_In_Local_Variable_Uses_Known_Value_Length(string declaration, bool diagnosticExpected)
    {
        VerifyMessage("value", diagnosticExpected, declaration);
    }

    [TestMethod]
    [DataRow("var value = \"123456789\";", "string.Format(\"{0}\", value)", true)]
    [DataRow("var value = \"1234567890\";", "string.Format(\"{0}\", value)", false)]
    [DataRow("const string value = \"123456789\";", "string.Format(\"{0}\", value)", true)]
    [DataRow("const string value = \"1234567890\";", "string.Format(\"{0}\", value)", false)]
    [DataRow("const string value = \"123456789\";", "$\"{value}\"", true)]
    [DataRow("const string value = \"123456789\";", "\"{Value}\", value", true)]
    [DataRow("const string value = \"12345\";", "value + \"6789\"", true)]
    [DataRow("var value = \"12345\";", "string.Concat(value, \"6789\")", true)]
    [DataRow("var value = \"12345\";", "string.Concat(value, \"67890\")", false)]
    [DataRow("var format = \"{0}\"; var value = \"123456789\";", "string.Format(format, value)", true)]
    [DataRow("var original = \"123456789\"; var value = original;", "value", true)]
    [DataRow("var value = string.Format(\"{0}\", \"123456789\");", "value", true)]
    public void String_Construction_Resolves_Constants_And_Unchanged_Locals(
        string declaration, string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected, declaration);
    }

    [TestMethod]
    [DataRow("value")]
    [DataRow("$\"{value}\"")]
    [DataRow("\"{Value}\", value")]
    [DataRow("\"1\" + value")]
    [DataRow("string.Format(\"{0}\", value)")]
    [DataRow("string.Concat(\"1\", value)")]
    public void No_Diagnostic_For_Reassigned_Local(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false,
            declaration: "var value = \"1\"; value = input;");
    }

    [TestMethod]
    [DataRow("var value = \"1\"; if (input != null) value = input;")]
    [DataRow("var value = \"1\"; System.Action change = () => value = input; change();")]
    [DataRow("var value = \"1\"; ref var alias = ref value; alias = input;")]
    [DataRow("var value = \"1\"; void Change(ref string text) { text = input; } Change(ref value);")]
    [DataRow("var original = \"1\"; var value = original; original = input;")]
    [DataRow("string value = \"1\", other = (value = input);")]
    [DataRow("string GetValue() => input; var value = GetValue();")]
    public void No_Diagnostic_For_Local_With_Uncertain_Value(string declaration)
    {
        VerifyMessage("value", diagnosticExpected: false, declaration);
    }

    [TestMethod]
    [DataRow("$\"{value,9}\"", true)]
    [DataRow("$\"{value,10}\"", false)]
    [DataRow("\"{Value,9}\", value", true)]
    [DataRow("\"{Value,10}\", value", false)]
    [DataRow("\"{{{Value}}}\", value", true)]
    public void Known_Local_Uses_Formatted_Length(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, "var value = \"1\";");
    }

    [TestMethod]
    public void No_Diagnostic_For_Message_In_Unresolved_Method_Parameter()
    {
        VerifyMessage("input", diagnosticExpected: false);
    }

    [TestMethod]
    [DataRow("value")]
    [DataRow("$\"{value}\"")]
    [DataRow("\"{Value}\", value")]
    [DataRow("\"1\" + value")]
    [DataRow("string.Format(\"{0}\", value)")]
    [DataRow("string.Concat(\"1\", value)")]
    [DataRow("$\"{known}{value}\"")]
    [DataRow("\"{Known}{Value}\", known, value")]
    [DataRow("known + value")]
    public void No_Diagnostic_When_Local_Variable_Value_Cannot_Be_Resolved(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false, declaration: "var known = \"1\"; var value = input;");
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
    [DataRow("\"12345678\" +\n    input", false)]
    [DataRow("\"123456789\" +\n    input", false)]
    [DataRow("\"12345678\" +\r\n\tinput", false)]
    [DataRow("\"123456789\" +\r\n\tinput", false)]
    [DataRow("$\"12345678{\n    input}\"", false)]
    [DataRow("$\"123456789{\n    input}\"", false)]
    [DataRow("$\"12345678{\r\n\tinput}\"", false)]
    [DataRow("$\"123456789{\r\n\tinput}\"", false)]
    [DataRow("\n    \"12345678{Value}\",\n    input", false)]
    [DataRow("\n    \"123456789{Value}\",\n    input", false)]
    [DataRow("\r\n\t\"12345678{Value}\",\r\n\tinput", false)]
    [DataRow("\r\n\t\"123456789{Value}\",\r\n\tinput", false)]
    [DataRow("string.Format(\n    \"12345678{0}\",\n    input)", false)]
    [DataRow("string.Format(\n    \"123456789{0}\",\n    input)", false)]
    [DataRow("string.Format(\r\n\t\"12345678{0}\",\r\n\tinput)", false)]
    [DataRow("string.Format(\r\n\t\"123456789{0}\",\r\n\tinput)", false)]
    public void Source_Line_Breaks_And_Indentation_Do_Not_Count_Towards_Message_Length(
        string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("var value =\n    \"123456789\";", "\n    value", true)]
    [DataRow("var value =\n    \"1234567890\";", "\n    value", false)]
    [DataRow("var value =\r\n\t\"123456789\";", "\r\n\tvalue", true)]
    [DataRow("var value =\r\n\t\"1234567890\";", "\r\n\tvalue", false)]
    public void Multiline_Local_Declaration_And_Log_Argument_Use_Known_Value_Length(
        string declaration, string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
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
    [DataRow("\"Got {Id}\", (object)null", true)]
    [DataRow("\"Got {Id}\", \"\"", true)]
    [DataRow("\"{Id,-9}\", \"1\"", true)]
    [DataRow("\"{Id,-10}\", \"1\"", false)]
    [DataRow("\"{{Id}} {Id}\", \"1234\"", true)]
    [DataRow("\"{{Id}} {Id}\", \"12345\"", false)]
    public void Structured_Logging_Uses_Rendered_Values_In_Argument_Order(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\"Order {Id}\", 123", true)]
    [DataRow("\"Order {Id}\", 1234", false)]
    [DataRow("\"{Id:D9}\", 42", false)] // Doesn't have to work, just not throw an exception. The analyzer doesn't know the format at compile time.
    [DataRow("\"{Id:D10}\", 42", false)]
    [DataRow("\"Ok {Success}\", true", true)]
    [DataRow("\"Done {Success}\", false", false)]
    [DataRow("$\"Order {123}\"", true)]
    [DataRow("$\"Order {1234}\"", false)]
    [DataRow("$\"{42:D9}\"", false)] // Doesn't have to work, just not throw an exception. The analyzer doesn't know the format at compile time.
    [DataRow("$\"{42:D10}\"", false)]
    [DataRow("string.Format(\"Order {0}\", 123)", true)]
    [DataRow("string.Format(\"Order {0}\", 1234)", false)]
    public void Constant_Numeric_And_Boolean_Values_Use_Their_Formatted_Length(
            string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("string.Concat(\"Got \", \"12\", \"345\")", true)]
    [DataRow("string.Concat(\"Got \", \"12\", \"3456\")", false)]
    [DataRow("string.Concat(new string[] { \"Got \", \"12345\" })", true)]
    [DataRow("string.Concat(new string[] { \"Got \", \"123456\" })", false)]
    [DataRow("string.Concat(str1: \"12345\", str0: \"Got \" )", true)]
    [DataRow("string.Format(\"{{{0}}}\", \"1234567\")", true)]
    [DataRow("string.Format(\"{{{0}}}\", \"12345678\")", false)]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0:D9}\", 42)", false)] // Doesn't have to work, just not throw an exception. The analyzer doesn't know the culture at compile time.
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0:D10}\", 42)", false)]
    public void Common_Formatting_Overloads_Use_Their_Result_Length(string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\"Got {Id}\", input")]
    [DataRow("\"{Known} {Unknown}\", \"OK\", input")]
    [DataRow("\"Got {Id}\", new object[] { input }")]
    [DataRow("\"Got {Id}\", new object()")]
    [DataRow("\"At {Time}\", System.DateTime.UtcNow")]
    [DataRow("\"Id {Id}\", System.Guid.NewGuid()")]
    [DataRow("string.Concat(\"Got \", input, \"!\")")]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0}\", input)")]
    public void No_Diagnostic_For_Structured_Or_Formatted_Runtime_Values(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false);
    }

    [TestMethod]
    [DataRow("\"Got {Id}\", values", "var values = new object[] { \"12345\" };", false)] // Doesn't have to work, just not throw an exception. The analyzer doesn't know the length of the array at compile time.
    [DataRow("\"Got {Id}\", values", "var values = new object[] { \"123456\" };", false)]
    [DataRow("\"Got {Id}\", values", "var values = new object[] { \"1\" }; values[0] = input;", false)]
    [DataRow("value", "var value = input ?? \"Started\";", false)]
    [DataRow("value", "var value = input == null ? \"Started\" : input;", false)]
    public void Local_Message_And_Argument_Arrays_Respect_Runtime_Uncertainty(
        string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
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

