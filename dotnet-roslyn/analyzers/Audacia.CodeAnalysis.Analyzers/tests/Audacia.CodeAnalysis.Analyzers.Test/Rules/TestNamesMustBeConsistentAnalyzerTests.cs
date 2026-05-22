using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Audacia.CodeAnalysis.Analyzers.Rules.TestNamesMustBeConsistent;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class TestNamesMustBeConsistentAnalyzerTests : CodeFixVerifier
    {

        private readonly Mock<ISettingsReader> _mockSettingsReader = new();

        private void SetupConfiguredFormat(string configuredFormat)
        {
            _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(TestNamesMustBeConsistentAnalyzer.Id, TestNamesMustBeConsistentAnalyzer.FormatConfigKey)))
            .Returns(configuredFormat);
        }


        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TestNamesMustBeConsistentAnalyzer(_mockSettingsReader.Object);
        }

        internal static DiagnosticResult BuildExpectedResult(int lineNumber, int column, string methodName, string configuredFormat)
        {

            var diagnosticResult = new DiagnosticResult
            {
                Id = TestNamesMustBeConsistentAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = $"Test method name '{methodName}' is not consistent with configured format '{configuredFormat}'",
                Locations =
                    new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
        }

        [TestMethod]
        public void No_Diagnostic_For_Empty_Code()
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
        public void No_Diagnostic_For_Non_Test_Method()
        {

            SetupConfiguredFormat("/^[0-9]+$/");

            var testCode = @"
namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    public void NonTestMethod()
    {
    }
}";
            VerifyNoDiagnostic(testCode);

        }

        [TestMethod]
        [DataRow("^[a-zA-Z]+(_[a-zA-Z]+)+$", "Method_Name")]
        [DataRow("^[a-zA-Z]+(_[a-zA-Z]+)+$", "method_name")]
        [DataRow("\\d+", "Test12345")]
        public void No_Diagnostic_For_Test_Method_Name_Consistent_With_Configured_Format(string pattern, string methodName)
        {
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void " + methodName + @"()
    {
    }
}";

            VerifyNoDiagnostic(testCode);

        }

        [TestMethod]
        public void No_Diagnostic_When_No_Format_Configured()
        {
            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void AnythingGoes()
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        [DataRow("^[a-zA-Z]+(_[a-zA-Z]+)+$", "Method_Name")]
        [DataRow("\\d+", "Test12345")]
        public void No_Diagnostic_For_Theory_Method_Name_Consistent_With_Configured_Format(string pattern, string methodName)
        {
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Theory]
    [InlineData(1)]
    public void " + methodName + @"(int value)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostic_For_Non_Xunit_Test_Attribute()
        {
            SetupConfiguredFormat("^[a-zA-Z]+(_[a-zA-Z]+)+$");

            var testCode = @"
using System;

namespace ConsoleApplication1;

[AttributeUsage(AttributeTargets.Method)]
public class CustomTestAttribute : Attribute { }

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [CustomTest]
    public void MethodName()
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostic_For_Invalid_Regex_Pattern()
        {
            SetupConfiguredFormat("^[a-zA-Z]+(_[a-zA-Z]+)+$[");

            var testCode = @"
using System;
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void MethodName()
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }


        [TestMethod]
        [DataRow(" ^ [a-zA-Z]+(_[a-zA-Z]+)+$", "MethodName")]
        [DataRow("^[a-zA-Z]+(_[a-zA-Z]+)+$", "method_name_")]
        [DataRow("\\d+", "MethodName")]
        public void Diagnostic_For_Test_Method_Name_Not_Consistent_With_Configured_Format(string pattern, string methodName)
        {
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void " + methodName + @"()
    {
    }
}";

            var expected = BuildExpectedResult(13, 17, methodName, pattern);
            VerifyDiagnostic(testCode, expected);
        }        

        [TestMethod]
        public void Diagnostic_Only_For_Non_Compliant_Method_When_Multiple_Test_Methods_Exist()
        {
            var pattern = "^[a-zA-Z]+(_[a-zA-Z]+)+$";
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void Compliant_Method_Name()
    {
    }

    [Fact]
    public void NonCompliantMethodName()
    {
    }
}";

            var expected = BuildExpectedResult(18, 17, "NonCompliantMethodName", pattern);
            VerifyDiagnostic(testCode, expected);
        }

        [TestMethod]
        public void Diagnostic_For_All_Non_Compliant_Methods_When_Multiple_Test_Methods_Exist()
        {
            var pattern = "^[a-zA-Z]+(_[a-zA-Z]+)+$";
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void NonCompliantMethodName()
    {
    }

    [Fact]
    public void NonCompliantMethodName2()
    {
    }
}";

            var expectedOne = BuildExpectedResult(13, 17, "NonCompliantMethodName", pattern);
            var expectedTwo = BuildExpectedResult(18, 17, "NonCompliantMethodName2", pattern);
            VerifyDiagnostic(testCode, expectedOne, expectedTwo);
        }

        [TestMethod]
        [DataRow("Theory")]
        [DataRow("Xunit.Fact")]
        [DataRow("Xunit.Theory")]
        [DataRow("FactAttribute")]
        [DataRow("TheoryAttribute")]
        [DataRow("Xunit.FactAttribute")]
        [DataRow("Xunit.TheoryAttribute")]
        public void Diagnostic_For_Other_Xunit_Method_Attributes(string attributeName)
        {
            var pattern = "^[a-zA-Z]+(_[a-zA-Z]+)+$";
            SetupConfiguredFormat(pattern);

            var testCode = @"
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [" + attributeName + @"]
    public void NonCompliantMethodName()
    {
    }
}";

            var expectedOne = BuildExpectedResult(13, 17, "NonCompliantMethodName", pattern);
            VerifyDiagnostic(testCode, expectedOne);
        }
    }
}
