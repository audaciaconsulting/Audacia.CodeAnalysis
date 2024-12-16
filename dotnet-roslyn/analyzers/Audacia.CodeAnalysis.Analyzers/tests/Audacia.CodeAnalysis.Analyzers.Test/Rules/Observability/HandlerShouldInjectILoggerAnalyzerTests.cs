using Audacia.CodeAnalysis.Analyzers.Rules.Observability.HandlerShouldInjectILogger;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.Observability;

[TestClass]
public class HandlerShouldInjectILoggerAnalyzerTests : DiagnosticVerifier
{
    private readonly Mock<ISettingsReader> _mockSettingsReader = new Mock<ISettingsReader>();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new HandlerShouldInjectILoggerAnalyzer(_mockSettingsReader.Object);
    }

    private DiagnosticResult BuildExpectedResult(string memberName, int lineNumber, int column)
    {
        return new DiagnosticResult
        {
            Id = HandlerShouldInjectILoggerAnalyzer.Id,
            Message =
                $"Handler '{memberName}' should inject ILogger",
            Severity = DiagnosticSeverity.Warning,
            Locations =
                new[]
                {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                }
        };
    }

    [TestMethod]
    public void Diagnostics_For_Handler_Class_Constructor_Without_Injected_ILogger_And_With_Other_Parameters()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestHandler
    {
        public TestHandler(int i, int j)
        {
        }
    }
}";

        var expected = BuildExpectedResult(
            memberName: "TestHandler",
            lineNumber: 6,
            column: 16);

        // Act
        // Assert
        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostics_For_Handler_Class_Constructor_Without_Injected_ILogger_And_Without_Other_Parameters()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestHandler
    {
        public TestHandler()
        {
        }
    }
}";

        var expected = BuildExpectedResult(
            memberName: "TestHandler",
            lineNumber: 6,
            column: 16);

        // Act
        // Assert
        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void No_Diagnostics_For_Handler_Class_Constructor_With_Injected_ILogger()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestHandler
    {
        public TestHandler(ILogger logger)
        {
        }
    }
}";

        // Act
        // Assert
        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostics_For_Handler_Class_Constructor_With_Injected_Typed_ILogger()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestHandler
    {
        public TestHandler(ILogger<TestHandler> logger)
        {
        }
    }
}";

        // Act
        // Assert
        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostics_For_Handler_Class_Constructor_With_Injected_Typed_ILogger_And_Other_Parameters()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestHandler
    {
        public TestHandler(ILogger<TestHandler> logger, ICurrentUserProvider currentUserProvider)
        {
        }
    }
}";

        // Act
        // Assert
        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostics_For_Random_Class_Constructor_With_Injected_Typed_ILogger_And_Other_Parameters()
    {
        // Arrange
        var test = @"
namespace TestNamespace
{
    public class TestRandom
    {
        public TestRandom(ICurrentUserProvider currentUserProvider)
        {
        }
    }
}";

        // Act
        // Assert
        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void Diagnostics_For_Random_Class_Constructor_Without_Injected_Typed_ILogger()
    {
        // Arrange
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(HandlerShouldInjectILoggerAnalyzer.Id,
                    HandlerShouldInjectILoggerAnalyzer.HandlerIdentifyingTermsSettingKey)))
            .Returns("Random");

        var test = @"
namespace TestNamespace
{
    public class TestRandom
    {
        public TestRandom(ICurrentUserProvider currentUserProvider)
        {
        }
    }
}";

        var expected = BuildExpectedResult(
            memberName: "TestRandom",
            lineNumber: 6,
            column: 16);

        // Act
        // Assert
        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void
        No_Diagnostics_For_Random_Class_Constructor_With_Injected_Typed_ILogger_And_Other_Parameters_From_Config()
    {
        // Arrange
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(HandlerShouldInjectILoggerAnalyzer.Id,
                    HandlerShouldInjectILoggerAnalyzer.HandlerIdentifyingTermsSettingKey)))
            .Returns("Random");

        var test = @"
namespace TestNamespace
{
    public class TestRandom
    {
        public TestRandom(ILogger<TestRandom> logger, ICurrentUserProvider currentUserProvider)
        {
        }
    }
}";

        // Act
        // Assert
        VerifyNoDiagnostic(test);
    }
}