using Audacia.CodeAnalysis.Analyzers.Rules.ApplicationLogicShouldBeSignposted;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ApplicationLogicShouldBeSignpostedAnalyzerTests : DiagnosticVerifier
    {
        private readonly Mock<ISettingsReader> _mockSettingsReader = new();

        private void SetupHandlerSuffix(string handlerSuffix)
        {
            _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(ApplicationLogicShouldBeSignpostedAnalyzer.Id, ApplicationLogicShouldBeSignpostedAnalyzer.HandlerSuffixConfigKey)))
            .Returns(handlerSuffix);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ApplicationLogicShouldBeSignpostedAnalyzer(_mockSettingsReader.Object);
        }

        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string handlerName)
        {
            return new DiagnosticResult
            {
                Id = ApplicationLogicShouldBeSignpostedAnalyzer.Id,
                Message = $"Application logic handler '{handlerName}' does not inject a logger. LOG-04.6.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        [TestMethod]
        public void No_Diagnostic_For_Non_Handler()
        {
            var testCode = @"
namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);

        }

        [TestMethod]
        public void No_Diagnostic_For_Non_Handler_With_Different_Suffix()
        {
            SetupHandlerSuffix("Processor");

            var testCode = @"
namespace ConsoleApplication1;

public class TestProcessors
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        [DataRow("ILogger<TestProcessor>")]
        [DataRow("ILogger")]
        [DataRow("ILoggerFactory")]
        [DataRow("ILoggerProvider")]
        public void No_Diagnostic_For_Handler_With_Logger_Injected(string loggerType)
        {
            SetupHandlerSuffix("Processor");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public class TestProcessor
{
    private readonly " + loggerType + @" _logger;

    public TestProcessor(" + loggerType + @" logger)
    {
        _logger = logger;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_Without_Constructor_And_No_Logger_Injected()
        {
            SetupHandlerSuffix("Processor");
            var testCode = @"
namespace ConsoleApplication1;

public class TestProcessor
{
    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(4, 14, "TestProcessor");
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_With_Constructor_And_No_Logger_Injected()
        {
            SetupHandlerSuffix("Processor");
            var testCode = @"
namespace ConsoleApplication1;

public class TestProcessor
{
    private readonly string _parameterValue;

    public TestProcessor(string parameterValue)
    {
        _parameterValue = parameterValue;
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(4, 14, "TestProcessor");
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        [DataRow("ILogger<TestProcessor>")]
        [DataRow("ILogger")]
        [DataRow("ILoggerFactory")]
        [DataRow("ILoggerProvider")]
        public void No_Diagnostic_For_Handler_With_Primary_Constructor_And_Logger_Injected(string loggerType)
        {
            SetupHandlerSuffix("Processor");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public class TestProcessor(" + loggerType + @" logger)
{
    private readonly " + loggerType + @" _logger = logger;
    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_With_Primary_Constructor_And_No_Logger_Injected()
        {
            SetupHandlerSuffix("Processor");
            var testCode = @"
namespace ConsoleApplication1;

public class TestProcessor(string parameterValue)
{
    private readonly string _parameterValue = parameterValue;

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(4, 14, "TestProcessor");
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        [DataRow("ILogger<TestProcessor>")]
        [DataRow("ILogger")]
        [DataRow("ILoggerFactory")]
        [DataRow("ILoggerProvider")]
        public void No_Diagnostic_For_Handler_With_Logger_Injected_In_Partial_Class(string typedLogger)
        {
            SetupHandlerSuffix("Processor");
            var declarationCode = @"
namespace ConsoleApplication1;

public partial class TestProcessor
{
    static void Main(string[] args)
    {
    }
}";
            var constructorCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public partial class TestProcessor
{
    private readonly " + typedLogger + @" _logger;

    public TestProcessor(" + typedLogger + @" logger)
    {
        _logger = logger;
    }
}";

            VerifyNoDiagnostic([declarationCode, constructorCode]);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_With_No_Logger_Injected_In_Partial_Class()
        {
            SetupHandlerSuffix("Processor");
            var declarationCode = @"
namespace ConsoleApplication1;

public partial class TestProcessor
{
    static void Main(string[] args)
    {
    }
}";
            var constructorCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public partial class TestProcessor
{
    private readonly string _parameterValue;

    public TestProcessor(string parameterValue)
    {
        _parameterValue = parameterValue;
    }
}";

            var expected = BuildExpectedResult(4, 22, "TestProcessor");

            VerifyDiagnostic([declarationCode, constructorCode], expected);
        }


        [TestMethod]
        public void No_Diagnostic_For_Abstract_Handler_With_No_Logger_Injected()
        {
            SetupHandlerSuffix("Processor");

            var testCode = @"
namespace ConsoleApplication1;

public abstract class TestProcessor
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_With_Default_Suffix_When_No_Suffix_Configured()
        {
            var testCode = @"
namespace ConsoleApplication1;

public class TestHandler
{
    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(4, 14, "TestHandler");
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Each_Handler_Matching_Multiple_Configured_Suffixes()
        {
            SetupHandlerSuffix("Handler, Processor");
            var testCode = @"
namespace ConsoleApplication1;

public class TestHandler
{
}

public class TestProcessor
{
}

public class TestService
{
    static void Main(string[] args)
    {
    }
}";

            var expectedHandler = BuildExpectedResult(4, 14, "TestHandler");
            var expectedProcessor = BuildExpectedResult(8, 14, "TestProcessor");
            VerifyDiagnostic(testCode, expectedHandler, expectedProcessor);
        }

        [TestMethod]
        public void No_Diagnostic_For_Static_Handler()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
namespace ConsoleApplication1;

public static class TestHandler
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        [DataRow("public interface TestHandler { }")]
        [DataRow("public struct TestHandler { }")]
        [DataRow("public enum TestHandler { None }")]
        [DataRow("public delegate void TestHandler();")]
        public void No_Diagnostic_For_Non_Class_Handler(string typeDeclaration)
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
namespace ConsoleApplication1;

" + typeDeclaration + @"

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        [DataRow("public record TestHandler(string Value);")]
        [DataRow("public record class TestHandler(string Value);")]
        [DataRow("public record struct TestHandler(string Value);")]
        public void No_Diagnostic_For_Record_Handler(string typeDeclaration)
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
namespace ConsoleApplication1;

" + typeDeclaration + @"

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostic_For_Handler_With_Multiple_Constructors_And_One_Injecting_Logger()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public class TestHandler
{
    private readonly ILogger<TestHandler> _logger;

    public TestHandler()
    {
    }

    public TestHandler(ILogger<TestHandler> logger)
    {
        _logger = logger;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostic_For_Handler_With_Logger_Injected_Alongside_Other_Parameters()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public interface IRepository
{
}

public class TestHandler
{
    private readonly IRepository _repository;
    private readonly ILogger<TestHandler> _logger;

    public TestHandler(IRepository repository, ILogger<TestHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostic_For_Handler_With_Logger_Of_Different_Generic_Type_Injected()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public class OtherClass
{
}

public class TestHandler
{
    private readonly ILogger<OtherClass> _logger;

    public TestHandler(ILogger<OtherClass> logger)
    {
        _logger = logger;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_With_Custom_ILogger_Type_From_Different_Namespace_Injected()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
namespace CustomLogging
{
    public interface ILogger
    {
    }
}

namespace ConsoleApplication1
{
    using CustomLogging;

    public class TestHandler
    {
        private readonly ILogger _logger;

        public TestHandler(ILogger logger)
        {
            _logger = logger;
        }

        static void Main(string[] args)
        {
        }
    }
}";

            var expected = BuildExpectedResult(13, 18, "TestHandler");
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        public void No_Diagnostic_For_Handler_With_Custom_Logger_Implementation_Injected()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
using System;
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public class CustomLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
}

public class TestHandler
{
    private readonly CustomLogger _logger;

    public TestHandler(CustomLogger logger)
    {
        _logger = logger;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Handler_When_Only_Base_Class_Injects_Logger()
        {
            SetupHandlerSuffix("Handler");
            var testCode = @"
using Microsoft.Extensions.Logging;
namespace ConsoleApplication1;

public abstract class BaseHandler
{
    protected BaseHandler(ILogger logger)
    {
    }
}

public class TestHandler : BaseHandler
{
    public TestHandler() : base(null)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(12, 14, "TestHandler");
            VerifyDiagnostic(testCode, expected);
        }
    }
}
