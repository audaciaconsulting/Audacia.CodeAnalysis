using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNamedPropertiesPascalCase;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class LogMessagesNamedPropertiesPascalCaseAnalyzerTests : DiagnosticVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string propertyName)
        {
            return new DiagnosticResult
            {
                Id = LogMessagesNamedPropertiesPascalCaseAnalyzer.Id,
                Message = $"Log message property '{propertyName}' does not use PascalCase",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LogMessagesNamedPropertiesPascalCaseAnalyzer();
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
        [DataRow("CalculatedValue")]
        [DataRow("@CalculatedValue")]
        [DataRow("CLCValue")]
        [DataRow("X")]
        [DataRow("")]
        public void No_Diagnostics_When_Log_Message_Properties_Pascal_Case(string propertyName)
        {
            var test = @"
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
        logger.LogInformation(""Calculated value: {" + propertyName + @"}"", x);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Log_Message_Uses_Positional_Properties()
        {
            var test = @"
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
        var y = x + 3;
        logger.LogInformation(""Calculated value: {0} {1:N0}"", x, y);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Log_Method_Not_Analyzed()
        {
            var test = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;

class ClassName
{
    static void Main(string[] args)
    {
    }
    
    private void Method()
    {
        var logger = new CustomLogger();

        var x = 1 + 2;
        logger.LogInformation(""Calculated value: {calculatedValue}"", x);
    }
}

class CustomLogger
{
    public void LogInformation(string message, params object[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        [DataRow("calculatedValue")]
        [DataRow("@calculatedValue")]
        [DataRow("calculated-value")]
        [DataRow("Calculated_Value")]
        [DataRow("Calculated!Value")]
        [DataRow("x")]
        [DataRow(" CalculatedValue ")]
        [DataRow("1CalculatedValue")]
        public void Diagnostic_When_Log_Message_Property_Not_Pascal_Case(string propertyName)
        {
            var test = @"
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
        logger.LogInformation(""Calculated value: {" + propertyName + @"}"", x);
    }
}";

            var expected = BuildExpectedResult(16, 50, propertyName);
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        // Log
        [DataRow("Log(LogLevel.Debug, new EventId(123), new System.Exception(), ")]
        [DataRow("Log(LogLevel.Debug, new EventId(123), ")]
        [DataRow("Log(LogLevel.Debug, new System.Exception(), ")]
        [DataRow("Log(LogLevel.Debug, ")]
        // LogCritical
        [DataRow("LogCritical(new EventId(123), new System.Exception(), ")]
        [DataRow("LogCritical(new EventId(123), ")]
        [DataRow("LogCritical(new System.Exception(), ")]
        [DataRow("LogCritical(")]
        // LogDebug
        [DataRow("LogDebug(new EventId(123), new System.Exception(), ")]
        [DataRow("LogDebug(new EventId(123), ")]
        [DataRow("LogDebug(new System.Exception(), ")]
        [DataRow("LogDebug(")]
        // LogError
        [DataRow("LogError(new EventId(123), new System.Exception(), ")]
        [DataRow("LogError(new EventId(123), ")]
        [DataRow("LogError(new System.Exception(), ")]
        [DataRow("LogError(")]
        // LogInformation
        [DataRow("LogInformation(new EventId(123), new System.Exception(), ")]
        [DataRow("LogInformation(new EventId(123), ")]
        [DataRow("LogInformation(new System.Exception(), ")]
        [DataRow("LogInformation(")]
        // LogTrace
        [DataRow("LogTrace(new EventId(123), new System.Exception(), ")]
        [DataRow("LogTrace(new EventId(123), ")]
        [DataRow("LogTrace(new System.Exception(), ")]
        [DataRow("LogTrace(")]
        // LogWarning
        [DataRow("LogWarning(new EventId(123), new System.Exception(), ")]
        [DataRow("LogWarning(new EventId(123), ")]
        [DataRow("LogWarning(new System.Exception(), ")]
        [DataRow("LogWarning(")]
        public void Diagnostic_With_Different_Logger_Method_Overloads(string overloadPrefix)
        {
            var test = @"
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
        logger." + overloadPrefix + @"""Calculated value: {calculatedValue}"", x);
    }
}";
            var logger = (new LoggerFactory()).CreateLogger<LogMessagesNamedPropertiesPascalCaseAnalyzerTests>();


            var expected = BuildExpectedResult(16, 35 + overloadPrefix.Length, "calculatedValue");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Log_Message_Properties_Not_Pascal_Case()
        {
            var test = @"
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
        logger.LogInformation(""Calculated value: {valueOne} and {Value_Two}"", x);
    }
}";

            var expectedOne = BuildExpectedResult(16, 50, "valueOne");
            var expectedTwo = BuildExpectedResult(16, 65, "Value_Two");
            VerifyDiagnostic(test, expectedOne, expectedTwo);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Multiple_Log_Message_Properties_Not_Pascal_Case()
        {
            var test = @"
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
        logger.LogInformation(""Calculated value: {valueOne} and {Value_Two}"", x);
        logger.LogDebug(""Calculated value: {value-three} and {Value!Four}"", x);
    }
}";

            var expectedOne = BuildExpectedResult(16, 50, "valueOne");
            var expectedTwo = BuildExpectedResult(16, 65, "Value_Two");
            var expectedThree = BuildExpectedResult(17, 44, "value-three");
            var expectedFour = BuildExpectedResult(17, 62, "Value!Four");

            VerifyDiagnostic(test, expectedOne, expectedTwo, expectedThree, expectedFour);
        }



    }
}
