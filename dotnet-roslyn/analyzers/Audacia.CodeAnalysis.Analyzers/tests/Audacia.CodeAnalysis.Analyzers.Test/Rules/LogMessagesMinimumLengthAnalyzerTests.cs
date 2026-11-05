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

    /// <summary>
    /// Argument values that aren't known at compile time can't be substituted, so the template is measured as written
    /// (including placeholder names), in the same way as <c>[LoggerMessage]</c> and <c>LoggerMessage.Define</c>.
    /// </summary>
    [TestMethod]
    [DataRow("\"{Value}\", input", "", true)]
    [DataRow("\"12{Value}\", input", "", true)]
    [DataRow("\"123{Value}\", input", "", false)]
    [DataRow("\"{A}{B}\", input, input", "", true)]
    [DataRow("\"1{First}{Second}\", input, input", "", false)]
    [DataRow("\"Got {Id}\", input", "", true)]
    [DataRow("\"Got {Id}\", new object()", "", true)]
    [DataRow("\"Got {Id}\", new object[] { input }", "", true)]
    [DataRow("\"At {Time}\", System.DateTime.UtcNow", "", true)]
    [DataRow("\"Id {Id}\", System.Guid.NewGuid()", "", true)]
    [DataRow("\"Processed {OrderId}\", input", "", false)]
    [DataRow("\"{Known} {Unknown}\", \"OK\", input", "", false)]
    [DataRow("\"{Value}\", value", "var value = \"1\"; value = input;", true)]
    [DataRow("\"{Value}\", value", "var value = input;", true)]
    [DataRow("\"{Known}{Value}\", known, value", "var known = \"1\"; var value = input;", false)]
    [DataRow("\"Got {Id}\", values", "var values = new object[] { \"123456\" };", true)]
    [DataRow("\"Got {Id}\", values", "var values = new object[] { \"1\" }; values[0] = input;", true)]
    public void Message_Template_With_Unknown_Values_Is_Measured_As_Written(
        string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    /// <summary>
    /// Values with format specifiers can't be substituted reliably, so the template is measured as written.
    /// </summary>
    [TestMethod]
    [DataRow("\"{Id:D9}\", 42", true)]
    [DataRow("\"{Id:D10}\", 42", true)]
    [DataRow("\"{Id:X}\", 42", true)]
    [DataRow("\"{Id,10:D9}\", 42", false)]
    [DataRow("\"{{literal}} {Id:D9}\", 42", false)]
    public void Message_Template_With_Format_Specifiers_Is_Measured_As_Written(string arguments, bool diagnosticExpected)
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
    [DataRow("string.Format(\"{0}\", value)")]
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
    [DataRow("var value = \"1\"; value += input;")]
    [DataRow("var value = \"1\"; value ??= input;")]
    [DataRow("var value = \"1\"; (value) = input;")]
    [DataRow("var value = \"1\"; void Set(out string text) { text = input; } Set(out value);")]
    [DataRow("var value = \"1\"; (value, var count) = (input, 1);")]
    [DataRow("var value = \"1\"; (var count, (value, var other)) = (1, (input, 2));")]
    [DataRow("var value = \"1\"; { value = input; }")]
    [DataRow("var value = \"1\"; void Show() => System.Console.WriteLine(value);")]
    public void No_Diagnostic_For_Local_With_Uncertain_Value(string declaration)
    {
        VerifyMessage("value", diagnosticExpected: false, declaration);
    }

    /// <summary>
    /// "Value: {Value}" is long enough as written, but too short when a known value of "1" is substituted.
    /// Known values must still be substituted where unknown values are detected without getting the invocation's operation.
    /// </summary>
    [TestMethod]
    [DataRow("var value = \"1\";", true)]
    [DataRow("var original = \"1\"; var value = original;", true)]
    [DataRow("var value = string.Concat(\"1\", \"\");", true)]
    [DataRow("var value = $\"{1}\";", true)]
    [DataRow("var value = \"\" + 1;", true)]
    [DataRow("var value = input.Length;", false)]
    [DataRow("var value = System.Math.Abs(-1);", false)]
    [DataRow("var value = 1 + 0 * input.Length;", false)]
    [DataRow("var original = input; var value = original;", false)]
    [DataRow("var value = input == null ? \"1\" : \"2\";", false)]
    public void Template_Arguments_Are_Substituted_Only_When_Known(string declaration, bool diagnosticExpected)
    {
        VerifyMessage("\"Value: {Value}\", value", diagnosticExpected, declaration);
    }

    /// <summary>
    /// As written, each message is long enough; if the changed local were resolved as "1", it would be too short.
    /// </summary>
    [TestMethod]
    [DataRow("var value = 1; value++;")]
    [DataRow("var value = 1; --value;")]
    [DataRow("var value = 1; unsafe { var pointer = &value; *pointer = 123; }")]
    public void No_Diagnostic_For_Changed_Numeric_Local(string declaration)
    {
        VerifyMessage("\"Count: \" + value", diagnosticExpected: false, declaration);
    }


    /// <summary>
    /// Outside padding should be trimmed, so these all evaluate as too short.
    ///  </summary>
    [TestMethod]
    [DataRow("$\"{value,9}\"")] // $"{text,n}" pads the string to a minimum length of n, so when 'value' = 1, this evaluates to "        1"
    [DataRow("$\"{value,10}\"")]
    [DataRow("\"{Value,9}\", value")]
    [DataRow("\"{Value,10}\", value")]
    [DataRow("\"{{{Value}}}\", value")]
    public void Known_Local_With_Only_Padding_Gets_Diagnostic(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: true, "var value = \"1\";");
    }


    /// <summary>
    /// Padding outside the message is trimmed, but padding between other text is part of the message, so it counts towards the length.
    /// </summary>
    [TestMethod]
    [DataRow("$\"1234567{value,2}\"", true)]
    [DataRow("$\"12345678{value,2}\"", false)]
    [DataRow("$\"{value,-2}1234567\"", true)] // The negative pad value adds padding at position 2, so when 'value' = 1, this evaluates to "1 1234567"
    [DataRow("$\"{value,-2}12345678\"", false)]
    [DataRow("\"1234567{Value,2}\", value", true)]
    [DataRow("\"12345678{Value,2}\", value", false)]
    [DataRow("\"{Value,-2}1234567\", value", true)]
    [DataRow("\"{Value,-2}12345678\", value", false)]
    public void Known_Local_Uses_Formatted_Length(string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, "var value = \"1\";");
    }


    /// <summary>
    /// Large alignments are capped while formatting, without changing whether the message is long enough.
    /// </summary>
    [TestMethod]
    [DataRow("$\"{value,999999}\"", true)] // Pads at the start
    [DataRow("$\"{value,-999999}\"", true)] // The negative pad value effectively pads at the end
    [DataRow("$\"1{value,999999}\"", false)]
    [DataRow("$\"{value,-999999}1\"", false)]
    [DataRow("\"{Value,999999}\", value", true)]
    [DataRow("\"1{Value,999999}\", value", false)]
    [DataRow("\"{Value,-999999}1\", value", false)]
    [DataRow("string.Format(\"{0,999999}\", value)", true)]
    [DataRow("string.Format(\"1{0,999999}\", value)", false)]
    public void Known_Local_Formatting_Caps_Large_Alignments(string arguments, bool diagnosticExpected)
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
    [DataRow("string.Format(\"{0}\", value)")]
    public void No_Diagnostic_When_Local_Variable_Value_Cannot_Be_Resolved(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false, declaration: "var known = \"1\"; var value = input;");
    }

    /// <summary>
    /// Messages built from strings with values that aren't known at compile time are measured as if written as an
    /// interpolated string, with each unknown value shown as <c>{its source text}</c>, like a template placeholder.
    /// </summary>
    [TestMethod]
    [DataRow("$\"12{input}\"", "", true)]
    [DataRow("$\"123{input}\"", "", false)]
    [DataRow("$\"12{\n    input}\"", "", true)]
    [DataRow("$\"123{\n    input}\"", "", false)]
    [DataRow("$\"1{i:N}\"", "var i = input;", true)]
    [DataRow("$\"1{input,3}\"", "", false)]
    [DataRow("\"12\" + input", "", true)]
    [DataRow("\"123\" + input", "", false)]
    [DataRow("input + \"12\"", "", true)]
    [DataRow("input + \"123\"", "", false)]
    [DataRow("string.Concat(\"12\", input)", "", true)]
    [DataRow("string.Concat(\"123\", input)", "", false)]
    [DataRow("string.Format(\"12{0}\", input)", "", true)]
    [DataRow("string.Format(\"123{0}\", input)", "", false)]
    [DataRow("string.Format(\"1{0:N}\", i)", "var i = input;", true)]
    [DataRow("string.Format(\"1{0:N}\", input)", "", false)]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"12{0}\", input)", "", true)]
    [DataRow("string.Join(\"\", \"12\", input)", "", true)]
    [DataRow("string.Join(\" \", \"12\", input)", "", false)]
    [DataRow("\"1\" + value", "var value = \"1\"; value = input;", true)]
    [DataRow("string.Concat(\"1\", value)", "var value = \"1\"; value = input;", true)]
    [DataRow("$\"{known}{value}\"", "var known = \"1\"; var value = input;", true)]
    [DataRow("known + value", "var known = \"1\"; var value = input;", true)]
    [DataRow("$\"Failed {input}\"", "", false)]
    public void Message_Built_From_Strings_With_Unknown_Values_Is_Measured_As_Written(
        string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    /// <summary>
    /// A message that's only stored in a variable, or is made up entirely of unknown values, has nothing meaningful
    /// to measure, so the rule is skipped.
    /// </summary>
    [TestMethod]
    [DataRow("message", "var message = $\"1{input}\";")]
    [DataRow("message", "var message = \"1\" + input;")]
    [DataRow("input + input", "")]
    [DataRow("$\"{input}{input}\"", "")]
    [DataRow("string.Concat(input, input)", "")]
    [DataRow("string.Format(\"{0}{1}\", input, input)", "")]
    [DataRow("\" \" + input", "")]
    [DataRow("string.Join(\", \", values)", "var values = new[] { input };")]
    [DataRow("string.Format(format, input)", "var format = input;")]
    public void No_Diagnostic_When_Message_Has_No_Known_Text(string arguments, string declaration)
    {
        VerifyMessage(arguments, diagnosticExpected: false, declaration);
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
    [DataRow("\"Order {Id}\", 123", true)]
    [DataRow("\"Order {Id}\", 1234", false)]
    [DataRow("\"Ok {Success}\", true", true)]
    [DataRow("\"Done {Success}\", false", false)]
    [DataRow("$\"Order {123}\"", true)]
    [DataRow("$\"Order {1234}\"", false)]
    [DataRow("string.Format(\"Order {0}\", 123)", true)]
    [DataRow("string.Format(\"Order {0}\", 1234)", false)]
    public void Constant_Numeric_And_Boolean_Values_Use_Their_Formatted_Length(
            string arguments, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("$\"{42:D9}\"")]
    [DataRow("$\"{42:D10}\"")]
    [DataRow("$\"{42:X}\"")]
    [DataRow("string.Format(\"{0:D9}\", 42)")]
    [DataRow("string.Format(\"{0:D10}\", 42)")]
    [DataRow("string.Format(\"{0:X}\", 42)")]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0:D9}\", 42)")]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0:D10}\", 42)")]
    public void No_Diagnostic_When_Format_Specifiers_Cannot_Be_Evaluated_Reliably(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false);
    }

    [TestMethod]
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

    [TestMethod]
    [DataRow("string.Concat(\"Got \", input, \"!\")")]
    [DataRow("string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0}\", input)")]
    public void No_Diagnostic_For_Structured_Or_Formatted_Runtime_Values(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false);
    }

    [TestMethod]
    [DataRow("value", "var value = input ?? \"Started\";", false)]
    [DataRow("value", "var value = input == null ? \"Started\" : input;", false)]
    public void Local_Message_And_Argument_Arrays_Respect_Runtime_Uncertainty(
        string arguments, string declaration, bool diagnosticExpected)
    {
        VerifyMessage(arguments, diagnosticExpected, declaration);
    }

    /// <summary>
    /// Each local doubles the previous one, so re-resolving every reference would take 2^10 local resolutions.
    /// </summary>
    [TestMethod]
    [Timeout(30_000)]
    public void Chained_Locals_Referenced_Many_Times_Are_Resolved_Once()
    {
        VerifyMessage("value10", diagnosticExpected: true, DoublingLocals("\"\"", 10));
    }

    /// <summary>
    /// Each local doubles the previous one, so the last would be 2^10 characters long. Resolution stops at the work limit
    /// and the message is skipped, rather than measured as written (e.g. <c>Hi {value10}</c>, which would be too short).
    /// </summary>
    [TestMethod]
    [Timeout(30_000)]
    [DataRow("value10")]
    [DataRow("\"Hi \" + value10")]
    [DataRow("$\"Hi {value10}\"")]
    public void No_Diagnostic_When_Resolved_Value_Exceeds_Work_Limit(string arguments)
    {
        VerifyMessage(arguments, diagnosticExpected: false, DoublingLocals("\"x\"", 10));
    }

    /// <summary>
    /// A long concatenation of unknown values exceeds the work limit, so rendering it as written stops rather than
    /// recursing through every operand (which could overflow the stack), and the message is skipped.
    /// </summary>
    [TestMethod]
    [Timeout(30_000)]
    public void No_Diagnostic_When_Long_Concatenation_Exceeds_Work_Limit()
    {
        var arguments = "\"1\"" + string.Concat(Enumerable.Repeat(" + input", 10_000));

        VerifyMessage(arguments, diagnosticExpected: false);
    }

    private static string DoublingLocals(string initialValue, int count)
    {
        var declarations = new System.Text.StringBuilder($"var value0 = {initialValue};");

        for (var index = 1; index <= count; index++)
        {
            declarations.Append($" var value{index} = value{index - 1} + value{index - 1};");
        }

        return declarations.ToString();
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
    [DataRow("Level = LogLevel.Information, Message = \"Processed {OrderId}\"", null, false)]
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
    [DataRow("LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), \"Processed {Id}\")", null, false)]
    [DataRow("LoggerMessage.Define(formatString: \"123456789\", eventId: new EventId(1), logLevel: LogLevel.Information)", "formatString: \"123456789\"", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), input)", null, false)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"12\" + input)", "\"12\" + input", true)]
    [DataRow("LoggerMessage.Define(LogLevel.Information, new EventId(1), \"123\" + input)", null, false)]
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
    /// Verifies a diagnostic is reported at the first occurrence of <paramref name="flaggedArgument"/>, or that none is reported.
    /// </summary>
    private void VerifyCodeAt(string testCode, string flaggedArgument, bool diagnosticExpected, int minLength = 10)
    {
        if (!diagnosticExpected)
        {
            VerifyNoDiagnostic(testCode);
            return;
        }

        var index = testCode.IndexOf(flaggedArgument, System.StringComparison.Ordinal);
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

