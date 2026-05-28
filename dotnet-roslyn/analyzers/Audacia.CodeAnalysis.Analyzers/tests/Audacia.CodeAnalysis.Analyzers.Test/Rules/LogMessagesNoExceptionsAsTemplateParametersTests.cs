using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNoExceptionsAsTemplateParameters;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class LogMessagesNoExceptionsAsTemplateParametersTests : DiagnosticVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string propertyName)
        {
            return new DiagnosticResult
            {
                Id = LogMessagesNoExceptionsAsTemplateParametersAnalyzer.Id,
                Message = $"Log message property '{propertyName}' is an exception",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LogMessagesNoExceptionsAsTemplateParametersAnalyzer();
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
        public void No_Diagnostic_When_Log_Message_Property_Not_Exception()
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
        logger.LogInformation(""Calculated value: {exception}"", x);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        // Log
        [DataRow("Log(LogLevel.Debug, new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("Log(LogLevel.Debug, new System.Exception(), \"Message\")")]
        // LogCritical
        [DataRow("LogCritical(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogCritical(new System.Exception(), \"Message\")")]
        // LogDebug
        [DataRow("LogDebug(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogDebug(new System.Exception(), \"Message\")")]
        // LogError
        [DataRow("LogError(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogError(new System.Exception(), \"Message\")")]
        // LogInformation
        [DataRow("LogInformation(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogInformation(new System.Exception(), \"Message\")")]
        // LogTrace
        [DataRow("LogTrace(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogTrace(new System.Exception(), \"Message\")")]
        // LogWarning
        [DataRow("LogWarning(new EventId(123), new System.Exception(), \"Message\")")]
        [DataRow("LogWarning(new System.Exception(), \"Message\")")]
        public void No_Diagnostic_When_Using_Exception_Overload(string methodOverload)
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
        logger." + methodOverload + @";
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

        var exception = new System.Exception(""Something went wrong"");
        logger.LogInformation(""Error: {exception}"", exception);
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
        public void Diagnostics_When_Log_Message_Uses_Exception_In_Positional_Properties()
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
        var anError = new System.Exception(""Something went wrong"");
        logger.LogInformation(""Calculated value: {0}, Error: {1}"", x, anError);
    }
}";
            var expected = BuildExpectedResult(17, 71, "anError");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Exception_Used_As_Template_Parameter()
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

        var exception = new System.Exception(""Something went wrong"");
        logger.LogInformation(""Message: {Error} {Value}"", exception, 123);
    }
}";
            var expected = BuildExpectedResult(16, 59, "exception");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Exception_Used_As_Template_Parameter_With_Serilog_Destructuring()
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

        var exception = new System.Exception(""Something went wrong"");
        logger.LogInformation(""Message: {@Error} {Value}"", exception, 123);
    }
}";
            var expected = BuildExpectedResult(16, 60, "exception");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Inline_Exception_Used_As_Template_Parameter()
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

        logger.LogInformation(""Message: {Error} {Value}"", new System.Exception(), 123);
    }
}";
            var expected = BuildExpectedResult(15, 59, "new System.Exception()");
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

        var anError = new System.Exception(""Something went wrong"");
        logger." + overloadPrefix + @"""The error: {Error}"", anError);
    }
}";

            var expected = BuildExpectedResult(16, 38 + overloadPrefix.Length, "anError");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        [DataRow("System.ArgumentException")]
        [DataRow("System.InvalidOperationException")]
        [DataRow("System.IO.IOException")]
        [DataRow("System.NullReferenceException")]
        [DataRow("System.TimeoutException")]
        [DataRow("System.UnauthorizedAccessException")]
        public void Diagnostic_When_Exception_Is_Inherited_From_System_Exception(string exceptionType)
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

        var exception = new " + exceptionType + @"();
        logger.LogInformation(""Message: {Error} {Value}"", exception, 123);
    }
}";
            var expected = BuildExpectedResult(16, 59, "exception");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Exception_Is_Custom_Type_Inherited_From_System_Exception()
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

        var error = new MyException();
        logger.LogInformation(""Error: {Error}"", error);
    }
}

class MyException : System.ArgumentException
{
}";
            var expected = BuildExpectedResult(16, 49, "error");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Exception_Is_Returned_From_Method_Used_As_Template_Parameter()
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

        logger.LogInformation(""Error: {Error}"", GetException());
    }

    private System.ArgumentException GetException()
    {
        return new System.ArgumentException();
    }
}";
            var expected = BuildExpectedResult(15, 49, "GetException()");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Log_Message_Properties_Are_Exceptions()
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

        var errorOne = new System.Exception();
        var errorTwo = new System.ArgumentException();
        logger.LogInformation(""Errors: {ErrorOne} and {ErrorTwo}"", errorOne, errorTwo);
    }
}";

            var expectedOne = BuildExpectedResult(17, 68, "errorOne");
            var expectedTwo = BuildExpectedResult(17, 78, "errorTwo");
            VerifyDiagnostic(test, expectedOne, expectedTwo);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Multiple_Log_Messages_Properties_Are_Exceptions()
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

        var errorOne = new System.Exception();
        var errorTwo = new System.ArgumentException();
        var errorThree = new System.InvalidOperationException();
        var errorFour = new System.IO.IOException();

        logger.LogInformation(""Errors: {ErrorOne} and {ErrorTwo}"", errorOne, errorTwo);
        logger.LogDebug(""Errors: {ErrorThree} and {ErrorFour}"", errorThree, errorFour);
    }
}";

            var expectedOne = BuildExpectedResult(20, 68, "errorOne");
            var expectedTwo = BuildExpectedResult(20, 78, "errorTwo");
            var expectedThree = BuildExpectedResult(21, 65, "errorThree");
            var expectedFour = BuildExpectedResult(21, 77, "errorFour");

            VerifyDiagnostic(test, expectedOne, expectedTwo, expectedThree, expectedFour);
        }
    }
}
