using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class BaseAssertionRequiredTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        internal static string BuildTestCode(string testMethod)
        {
            return @"
using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Shouldly;
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    " + testMethod + @"
}";
        }

        internal static DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {
            return new DiagnosticResult
            {
                Id = AssertionRequiredAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = "Test methods must contain at least one assertion",
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            var testCode = BuildTestCode("");

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Non_TestMethod()
        {
            const string testMethod = @"
public void NonTestMethod()
{
    var foo = ""Foo"";
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        [DataRow("Fact")]
        [DataRow("Theory")]
        [DataRow("Xunit.Fact")]
        [DataRow("Xunit.Theory")]
        [DataRow("FactAttribute")]
        [DataRow("TheoryAttribute")]
        [DataRow("Xunit.FactAttribute")]
        [DataRow("Xunit.TheoryAttribute")]
        public void Diagnostics_For_Empty_TestMethod(string attributeName)
        {
            var testMethod = @"
[" + attributeName + @"]
public void TestMethod()
{
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Fact_TestMethod_With_No_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_TestMethod_With_No_Assertions()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    var bar = ""Bar"";
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Fact_TestMethod_With_No_Assertions_In_Separate_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    NoAssertions(foo);
}

private void NoAssertions(string foo)
{
    foo = ""Bar"";
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_TestMethod_With_No_Assertions_In_Separate_Method()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    NoAssertions(foo);
}

private void NoAssertions(string foo)
{
    foo = ""Bar"";
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
