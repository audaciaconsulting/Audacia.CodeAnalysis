using System.Collections.Immutable;
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

    private DiagnosticResult BuildExpectedResult(int lineNumber, int column, int minLength)
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
    [DataRow("$\"{input}\"", true)]
    [DataRow("$\"12345678{input}\"", true)]
    [DataRow("$\"123456789{input}\"", false)]
    [DataRow("$\"1234567{input}{input}\"", true)]
    [DataRow("$\"12345678{input}{input}\"", false)]
    public void Interpolated_Method_Parameter_Counts_As_One_Character_Per_Occurrence(
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
    [DataRow("\"{Value}\", input", true)]
    [DataRow("\"12345678{Value}\", input", true)]
    [DataRow("\"123456789{Value}\", input", false)]
    [DataRow("\"1234567{First}{Second}\", input, input", true)]
    [DataRow("\"12345678{First}{Second}\", input, input", false)]
    public void Message_Template_With_Method_Parameter_Counts_As_One_Character_Per_Placeholder(
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
    [DataRow("\"12345678\" + input", true)]
    [DataRow("\"123456789\" + input", false)]
    [DataRow("input + \"12345678\"", true)]
    [DataRow("input + \"123456789\"", false)]
    [DataRow("\"1234567\" + input + input", true)]
    [DataRow("\"12345678\" + input + input", false)]
    public void Concatenated_Method_Parameter_Counts_As_One_Character_Per_Occurrence(
        string expression, bool diagnosticExpected)
    {
        VerifyMessage(expression, diagnosticExpected);
    }

    [TestMethod]
    [DataRow("\"1234\" + \"56789\"", true)]
    [DataRow("\"12345\" + \"67890\"", false)]
    [DataRow("string.Format(\"12345678{0}\", input)", true)]
    [DataRow("string.Format(\"123456789{0}\", input)", false)]
    [DataRow("string.Format(\"{0}\", \"123456789\")", true)]
    [DataRow("string.Format(\"{0}\", \"1234567890\")", false)]
    [DataRow("string.Concat(\"12345678\", input)", true)]
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
    public void Message_In_Method_Parameter_Counts_As_One_Character()
    {
        VerifyMessage("input", diagnosticExpected: true);
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
    [DataRow("\"12345678\" +\n    input", true)]
    [DataRow("\"123456789\" +\n    input", false)]
    [DataRow("\"12345678\" +\r\n\tinput", true)]
    [DataRow("\"123456789\" +\r\n\tinput", false)]
    [DataRow("$\"12345678{\n    input}\"", true)]
    [DataRow("$\"123456789{\n    input}\"", false)]
    [DataRow("$\"12345678{\r\n\tinput}\"", true)]
    [DataRow("$\"123456789{\r\n\tinput}\"", false)]
    [DataRow("\n    \"12345678{Value}\",\n    input", true)]
    [DataRow("\n    \"123456789{Value}\",\n    input", false)]
    [DataRow("\r\n\t\"12345678{Value}\",\r\n\tinput", true)]
    [DataRow("\r\n\t\"123456789{Value}\",\r\n\tinput", false)]
    [DataRow("string.Format(\n    \"12345678{0}\",\n    input)", true)]
    [DataRow("string.Format(\n    \"123456789{0}\",\n    input)", false)]
    [DataRow("string.Format(\r\n\t\"12345678{0}\",\r\n\tinput)", true)]
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

    /// <summary>
    /// Settings are read when the analyzer runs, and Roslyn reports any exception thrown by an analyzer as an AD0001 diagnostic.
    /// </summary>
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
            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, provider));
    }

    private void VerifyMessage(string arguments, bool diagnosticExpected, string declaration = "", int minLength = 10)
    {
        var testCode = $$"""
            using Microsoft.Extensions.Logging;
            namespace ConsoleApplication;
            class ClassName
            {
                private void Method(ILogger<ClassName> logger, string input)
                {
                    {{declaration}}
                    logger.LogInformation({{arguments}});
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
                .Single(invocation => invocation.Expression.ToString() == "logger.LogInformation")
                .ArgumentList.Arguments[0];
            var position = messageArgument.GetLocation().GetLineSpan().StartLinePosition;

            VerifyDiagnostic(testCode, BuildExpectedResult(position.Line + 1, position.Character + 1, minLength));
        }
        else
        {
            VerifyNoDiagnostic(testCode);
        }
    }
}

