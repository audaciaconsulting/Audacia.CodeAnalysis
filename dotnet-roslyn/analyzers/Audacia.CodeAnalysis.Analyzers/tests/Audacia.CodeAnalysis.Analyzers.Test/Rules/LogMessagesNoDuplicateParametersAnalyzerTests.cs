using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.LogMessagesNoDuplicateParameters;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class LogMessagesNoDuplicateParametersAnalyzerTests : DiagnosticVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string propertyName)
        {
            return new DiagnosticResult
            {
                Id = LogMessagesNoDuplicateParametersAnalyzer.Id,
                Message = $"Log message property '{propertyName}' is duplicated",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LogMessagesNoDuplicateParametersAnalyzer();
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
        public void No_Diagnostics_When_Log_Message_Properties_Not_Duplicated()
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
        logger.LogInformation(""Calculated value: {PropOne} {PropTwo}"", x, y);
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
        public void No_Diagnostics_When_Log_Message_Properties_Not_Duplicated_With_Escaped_Braces()
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
        logger.LogInformation(""{{Calculated value}}: {PropOne} {PropTwo}"", x, y);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Log_Message_Properties_Not_Duplicated_With_Interpolated_Placeholders()
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
        logger.LogInformation($""{{{{Calculated value}}}}: {{PropOne}} {{PropTwo}}"", x, y);
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_When_Log_Message_Properties_Not_Duplicated_With_Formatting()
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
        var logger = (new LoggerFactory()).CreateLogger<ClassName>();

        var x = 1 + 2;
        var y = x + 3;
        logger.LogInformation(""Calculated value: {PropOne:N0} {PropTwo:c}"", x, y);
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
        var y = x + 3;
        logger.LogInformation(""Calculated value: {Prop} {Prop}"", x, y);
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
        public void No_Diagnostics_When_Log_Method_Called_On_Type_Implementing_ILogger()
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
        var y = x + 3;
        logger.LogInformation(""Calculated value: {Property} {PropertyTwo}"", x, y);
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
        var y = x + 3;

        logger.LogInformation(args: new []{ x, y }, message: ""Message: {Value} and {ValueTwo}"");
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        [DataRow("Property")]
        [DataRow("@Property")]
        public void Diagnostic_When_Log_Message_Property_Is_Duplicated(string propertyName)
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
        logger.LogInformation(""Calculated value: {" + propertyName + @"} {" + propertyName + @"}"", x, y);
    }
}";

            var expected = BuildExpectedResult(17, 53 + propertyName.Length, propertyName);
            VerifyDiagnostic(test, expected);   
        }

        [TestMethod]
        public void Diagnostic_When_Log_Message_Property_Is_Duplicated_Case_Insensitive()
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
        logger.LogInformation(""Calculated value: {Property} {property}"", x, y);
    }
}";

            var expected = BuildExpectedResult(17, 61, "property");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_When_Log_Message_Property_Is_Duplicated_With_Escaped_Braces()
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
        logger.LogInformation(""{{Calculated value}}: {Property} {Property}"", x, y);
    }
}";
            var expected = BuildExpectedResult(17, 65, "Property");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_When_Log_Message_Property_Is_Duplicated_With_Interpolated_Placeholders()
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
        logger.LogInformation($""{{{{Calculated value}}}}: {{Property}} {{Property}}"", x, y);
    }
}";
            var expected = BuildExpectedResult(17, 72, "Property");
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
        var y = x + 3;
        logger." + overloadPrefix + @"""Calculated value: {Property} {Property}"", x, y);
    }
}";

            var expected = BuildExpectedResult(17, 46 + overloadPrefix.Length, "Property");
            VerifyDiagnostic(test, expected);
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
        var y = x + 3;
        logger.LogInformation(""Calculated value: {Property} {Property}"", x, y);
    }
}

class CustomLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}";

            var expected = BuildExpectedResult(18, 61, "Property");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_When_Log_Message_Property_Is_Duplicated_With_Formatting()
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
        var logger = (new LoggerFactory()).CreateLogger<ClassName>();

        var x = 1 + 2;
        var y = x + 3;
        logger.LogInformation(""Calculated value: {Property:N0} {Property:c}"", x, y);
    }
}";
            var expected = BuildExpectedResult(18, 64, "Property");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Multiple_Diagnostics_When_Multiple_Log_Message_Properties_Are_Duplicated()
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
        var z = y + 4;

        logger.LogInformation(""Calculated values: {Value} {Value} {Value}"", x, y, z);
        logger.LogDebug(""Calculated values: {Property} {Value} {Property}"", x, y, z);
        logger.LogWarning(""Calculated values: {Property} {Value}"", x, y);
    }
}";

            // "Calculated values: {Value} {Value} {Value}" - 2nd {Value} at col 59, 3rd at col 67
            var expectedOne = BuildExpectedResult(19, 59, "Value");
            var expectedTwo = BuildExpectedResult(19, 67, "Value");
            // "Calculated values: {Property} {Value} {Property}" - 2nd {Property} at col 64
            var expectedThree = BuildExpectedResult(20, 64, "Property");

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
        var y = x + 3;

        logger.LogInformation(args: new []{ x, y }, message: ""Message: {Value} and {Value}"");
    }
}";
            var expected = BuildExpectedResult(18, 84, "Value");
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_When_Log_Message_Is_Variable_With_Duplicate_Property()
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
        var message = ""Calculated value: {Value} and {Value}"";
        logger.LogInformation(message, x, x);
    }
}";
            var expected = BuildExpectedResult(15, 54, "Value");
            VerifyDiagnostic(test, expected);
        }
    }
}