using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason;
using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using FluentAssertions;
using Shouldly;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class AssertionReasonMustBeProvidedAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionReasonMustBeProvidedAnalyzer();

        private static string BuildTestCode(string testMethod)
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

        private static DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {

            var diagnosticResult = new DiagnosticResult
            {
                Id = AssertionReasonMustBeProvidedAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = "Assertions must contain a reason",
                Locations =
                    new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
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
    Assert.NotEmpty(""foo"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Xunit_Assertion_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assert.False(foo, ""Reason"");
    Assert.True(foo, ""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Xunit_Assertion_Without_Required_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Equal(""Foo"", foo);
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Shouldly_Assertion_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty(""Reason"");
    foo.ShouldBe<string>(""Foo"", ""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_FluentAssertions_Assertion_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty(""Reason"");
    foo.Should().Be(""Foo"", ""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_Theory_With_Assertion_Reason()
        {
            const string testMethod = @"
[Theory]
[InlineData(true)]
public void TestMethod(bool foo)
{
    Assert.True(foo, ""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Xunit_Assertion_Reason_In_Separate_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assertion(foo);
}

private void Assertion(bool foo)
{
    Assert.True(foo, ""Reason"");
}";
            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Shouldly_Assertion_Reason_In_Separate_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assertion(foo);
}

private void Assertion(string foo)
{
    foo.ShouldNotBeNullOrEmpty(""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_FluentAssertions_Assertion_Reason_In_Separate_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assertion(foo);
}

private void Assertion(string foo)
{
    foo.Should().NotBeEmpty(""Reason"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Xunit_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assert.True(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(21, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Shouldly_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(21, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_FluentAssertions_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(21, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Shouldly_Conditional_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo?.ShouldNotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(21, 9);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_FluentAssertions_Conditional_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo?.Should().NotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(21, 9);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Xunit_Assertion_With_And_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assert.False(foo, ""Reason"");
    Assert.True(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Shouldly_Assertion_With_And_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty(""Reason"");
    foo.ShouldBe<string>(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_FluentAssertions_Assertion_With_And_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty(""Reason"");
    foo.Should().Be(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Xunit_Assertion_In_Separate_Method_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assertion(foo);
}

private void Assertion(bool foo)
{
    Assert.True(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(26, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Shouldly_Assertion_In_Separate_Method_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assertion(foo);
}

private void Assertion(string foo)
{
    foo.ShouldNotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(26, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_FluentAssertions_Assertion_In_Separate_Method_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assertion(foo);
}

private void Assertion(string foo)
{
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(26, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Xunit_Multiple_Assertions_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assert.False(foo);
    Assert.True(foo);
    Assert.Fail();
}";
            var testCode = BuildTestCode(testMethod);
            var firstExpectedDiagnostic = BuildExpectedResult(21, 5);
            var secondExpectedDiagnostic = BuildExpectedResult(22, 5);
            var thirdExpectedDiagnostic = BuildExpectedResult(23, 5);

            VerifyDiagnostic(testCode, firstExpectedDiagnostic, secondExpectedDiagnostic, thirdExpectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Shouldly_Multiple_Assertions_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldBe<string>(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var firstExpectedDiagnostic = BuildExpectedResult(21, 5);
            var secondExpectedDiagnostic = BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, firstExpectedDiagnostic, secondExpectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_FluentAssertions_Multiple_Assertions_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
    foo.Should().Be(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var firstExpectedDiagnostic = BuildExpectedResult(21, 5);
            var secondExpectedDiagnostic = BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, firstExpectedDiagnostic, secondExpectedDiagnostic);
        }
    }
}
