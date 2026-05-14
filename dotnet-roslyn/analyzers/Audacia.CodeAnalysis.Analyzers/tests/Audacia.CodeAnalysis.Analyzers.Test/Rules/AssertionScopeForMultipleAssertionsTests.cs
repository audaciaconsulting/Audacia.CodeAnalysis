using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class AssertionScopeForMultipleAssertionsTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new AssertionScopeForMultipleAssertionsAnalyzer();

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
                Id = AssertionScopeForMultipleAssertionsAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = "Use an assertion scope when a test method has more than two assertions",
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
    Assert.NotEmpty(""foo"");
    Assert.NotEmpty(""foo"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Single_Xunit_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.NotEmpty(foo);
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Single_Shouldly_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Single_FluentAssertions_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_Theory_With_Single_Assertion()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    Assert.NotEmpty(foo);
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Two_Xunit_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.NotEmpty(foo);
    Assert.Equal(foo, ""Foo"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Two_Shouldly_Assertions()
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

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Two_FluentAssertions_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Xunit_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Multiple(() => {
        Assert.NotEmpty(foo);
        Assert.Equal(foo, ""Foo"");
        Assert.IsType<string>(foo);
    });
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Shouldly_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldSatisfyAllConditions(
        () => foo.ShouldNotBeNullOrEmpty(),
        () => foo.ShouldBe<string>(""Foo""),
        () => foo.ShouldNotBe(""Foo"")
    );
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_FluentAssestions_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using (new AssertionScope())
    {
        foo.Should().NotBeEmpty();
        foo.Should().Be(""Foo"");
        foo.Should().NotBe(""Bar"");
    }
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_FullyQualified_FluentAssestions_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using (new FluentAssertions.Execution.AssertionScope())
    {
        foo.Should().NotBeEmpty();
        foo.Should().Be(""Foo"");
        foo.Should().NotBe(""Bar"");
    }
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_FluentAssestions_InlineAssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using var _ = new AssertionScope();
    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");
}

[Fact]
public void TestMethod2()
{
    var foo = ""Foo"";
    using AssertionScope _ = new AssertionScope();
    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_TestMethod_With_Xunit_AssertionScope_With_Shouldly_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Multiple(() => {
        foo.ShouldNotBeNullOrEmpty();
        foo.ShouldBe<string>(""Foo"");
        foo.ShouldNotBe(""Foo"");
    });
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Without_Xunit_BlockAssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    Assert.NotEmpty(foo);
    Assert.Equal(foo, ""Foo"");
    Assert.IsType<string>(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_Multiple_Assertions_Without_Xunit_BlockAssertionScope()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    Assert.NotEmpty(foo);
    Assert.Equal(foo, ""Foo"");
    Assert.IsType<string>(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Without_Shouldly_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldBe<string>(""Foo"");
    foo.ShouldNotBe(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_Multiple_Assertions_Without_Shouldly_AssertionScope()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldBe<string>(""Foo"");
    foo.ShouldNotBe(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Without_FluentAssertions_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_Multiple_Assertions_Without_FluentAssertions_AssertionScope()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Outside_Xunit_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    Assert.NotEmpty(foo);

    Assert.Multiple(() => {
        Assert.NotEmpty(foo);
    });

    Assert.Equal(foo, ""Foo"");
    Assert.IsType<string>(foo);
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Outside_Shouldly_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo.ShouldNotBeNullOrEmpty();

    foo.ShouldSatisfyAllConditions(
        () => foo.ShouldNotBeNullOrEmpty()
    );

    foo.ShouldBe<string>(""Foo"");
    foo.ShouldNotBe(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Outside_FluentAssertions_BlockAssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo.Should().NotBeEmpty();

    using (new AssertionScope())
    {
        foo.Should().NotBeEmpty();
    }

    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Outside_FluentAssertions_InlineAssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeEmpty();
    foo.Should().Be(""Foo"");
    foo.Should().NotBe(""Bar"");

    using var _ = new AssertionScope();
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Conditional_Assertions_Without_FluentAssertions_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo?.Should().NotBeEmpty();
    foo?.Should().Be(""Foo"");
    foo?.Should().NotBe(""Bar"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Conditional_Assertions_Without_Shouldly_AssertionScope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";

    foo?.ShouldNotBeNullOrEmpty();
    foo?.ShouldBe<string>(""Foo"");
    foo?.ShouldNotBe(""Foo"");
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
