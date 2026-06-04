using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNamedPropertiesMustBeUsed;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class LogMessagesNamedPropertiesMustBeUsedAnalyzerTests : DiagnosticVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string propertyName)
        {
            return new DiagnosticResult
            {
                Id = LogMessagesNamedPropertiesMustBeUsedAnalyzer.Id,
                Message = $"Log message property '{propertyName}' is a positional parameter",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LogMessagesNamedPropertiesMustBeUsedAnalyzer();
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
        [DataRow("calculated_value")]
        [DataRow("calculated-value")]
        [DataRow("CalculatedValue1")]
        [DataRow("CLCValue")]
        [DataRow("x")]
        [DataRow("0calculatedValue")]
        [DataRow("0calculatedValue:N0")]
        [DataRow("Calculated:c")]
        [DataRow("")]
        public void No_Diagnostics_When_Log_Message_Properties_Not_Positional(string propertyName)
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
        logger.LogInformation(""123: {" + propertyName + @"}"", x);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Log_Message_Properties_Not_Positional_With_Escaped_Braces()
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
        logger.LogInformation(""{{123}}: {CalculatedValue}"", x);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        [DataRow("CalculatedValue")]
        [DataRow("@CalculatedValue")]
        [DataRow("CLCValue")]
        [DataRow("x")]
        [DataRow("0calculatedValue")]
        [DataRow("0calculatedValue:N0")]
        [DataRow("Calculated:c")]
        [DataRow("")]
        public void No_Diagnostics_When_Log_Message_Properties_Not_Positional_With_Interpolated_Placeholders(string propertyName)
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
        logger.LogInformation($""{{{{123}}}}: {{" + propertyName + @"}}"", x);
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
        public void No_Diagnostics_When_Log_Method_Called_On_Type_Implementing_ILogger_With_No_Positional_Properties()
        {
            var test = @"
using System;
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }
    private void Method()
    {
        ILogger logger = new CustomLogger();
        var x = 1 + 2;
        logger.LogInformation(""123: {CalculatedValue}"", x);
    }
}
class CustomLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Using_Arguments_Out_Of_Position()
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
        logger.LogInformation(args: new []{ x }, message: ""Message: {Value}"");
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_When_Log_Method_Called_On_Type_Implementing_ILogger()
        {
            var test = @"
using System;
using Microsoft.Extensions.Logging;
namespace ConsoleApplication;
class ClassName
{
    static void Main(string[] args)
    {
    }
    private void Method()
    {
        ILogger logger = new CustomLogger();
        var x = 1 + 2;
        logger.LogInformation(""Calculated value: {0}"", x);
    }
}
class CustomLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}";

            var expected = BuildExpectedResult(14, 50, "0");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        [DataRow("0")]
        [DataRow("@0")]
        [DataRow("123")]
        [DataRow("0:c")]
        [DataRow("0:N0")]
        public void Diagnostic_When_Log_Message_Uses_Positional_Property(string propertyName)
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

            var expected = BuildExpectedResult(13, 50, propertyName);
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_When_Log_Message_Uses_Positional_Property_With_Escaped_Braces()
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
        logger.LogInformation(""{{Calculated value}}: {0}"", x);
    }
}";
            var expected = BuildExpectedResult(14, 54, "0");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        [DataRow("0")]
        [DataRow("@0")]
        [DataRow("123")]
        [DataRow("0:c")]
        [DataRow("0:N0")]
        public void Diagnostic_When_Log_Message_Uses_Positional_Property_With_Interpolated_Placeholders(string propertyName)
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
        logger.LogInformation($""{{{{Calculated value}}}}: {{" + propertyName + @"}}"", x);
    }
}";

            var expected = BuildExpectedResult(13, 59, propertyName);
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
        logger." + overloadPrefix + @"""Calculated value: {0}"", x);
    }
}";

            var expected = BuildExpectedResult(13, 35 + overloadPrefix.Length, "0");
            VerifyDiagnostic(test, expected);
        }


        [TestMethod]
        public void Multiple_Diagnostics_When_Log_Message_Properties_Are_Positional()
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
        logger.LogInformation(""Calculated value: {0} and {1}"", x, y);
    }
}";

            var expectedOne = BuildExpectedResult(14, 50, "0");
            var expectedTwo = BuildExpectedResult(14, 58, "1");
            VerifyDiagnostic(test, expectedOne, expectedTwo);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Multiple_Log_Message_Properties_Are_Positional()
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
        logger.LogInformation(""Calculated value: {0} and {1}"", x, y);
        logger.LogDebug(""Calculated value: {value} and {0}"", x, y);
    }
}";

            var expectedOne = BuildExpectedResult(14, 50, "0");
            var expectedTwo = BuildExpectedResult(14, 58, "1");
            var expectedThree = BuildExpectedResult(15, 56, "0");

            VerifyDiagnostic(test, expectedOne, expectedTwo, expectedThree);
        }

        [TestMethod]
        public void Diagnostics_When_Using_Arguments_Out_Of_Position()
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
        logger.LogInformation(args: new []{ x }, message: ""Message: {0}"");
    }
}";
            var expected = BuildExpectedResult(14, 69, "0");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Log_Message_Uses_Positional_Property_In_Interpolated_String_With_Mixed_Content()
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
        logger.LogInformation($""Calculated value: {{0}} for {x}"", x);
    }
}";
            var expected = BuildExpectedResult(14, 51, "0");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Log_Message_Is_Variable_With_Positional_Property()
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
        var message = ""Calculated value: {0}"";
        logger.LogInformation(message, x);
    }
}";
            var expected = BuildExpectedResult(14, 42, "0");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        [DataRow("0,10")]
        [DataRow("0,10:N0")]
        public void Diagnostic_When_Log_Message_Uses_Positional_Property_With_Alignment_Or_Format(string propertyName)
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
        logger.LogInformation(""Value: {" + propertyName + @"}"", x);
    }
}";
            var expected = BuildExpectedResult(14, 39, propertyName);
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Log_Message_Uses_Positional_Property_With_Named_Args()
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
        logger.LogInformation(message: ""Calculated value: {0}"", args: new[] { x });
    }
}";
            var expected = BuildExpectedResult(14, 59, "0");
            VerifyDiagnostic(test, expected);
        }
    }
}