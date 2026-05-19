using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionScopeForMultipleAssertions
{
    [TestClass]
    public class ShouldlyAssertionScopeTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new AssertionScopeForMultipleAssertionsAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Non_TestMethod()
        {
            const string testMethod = @"
public void NonTestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldNotBeNullOrEmpty();
}";

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Single_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
}";

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Two_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldBe<string>(""Foo"");
}";

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_AssertionScope()
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

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Scope_In_Separate_Assertion_Method()
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
    foo.ShouldSatisfyAllConditions(
        () => foo.ShouldNotBeNullOrEmpty(),
        () => foo.ShouldBe<string>(""Foo""),
        () => foo.ShouldNotBe(""Foo"")
    );
}";
            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Scope_Outside_Separate_Assertion_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldSatisfyAllConditions(
        () => Assertion(foo)
    );
}

private void Assertion(string foo)
{
    foo.ShouldNotBeNullOrEmpty();
    foo.ShouldBe<string>(""Foo"");
    foo.ShouldNotBe(""Foo"");
}";
            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_TestMethod_With_Shouldly_AssertionScope_With_Xunit_Assertions()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.ShouldSatisfyAllConditions(
        () => Assert.NotEmpty(foo),
        () => Assert.Equal(foo, ""Foo""),
        () => Assert.IsType<string>(foo)
    );
}";

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Without_AssertionScope()
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

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Theory_Multiple_Assertions_Without_AssertionScope()
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

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(19, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Outside_AssertionScope()
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

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Conditional_Assertions_Without_AssertionScope()
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

            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_TestMethod_With_Separate_Assertions_Method()
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
    foo.ShouldBe<string>(""Foo"");
    foo.ShouldNotBe(""Foo"");
}";
            var testCode = BaseAssertionScopeTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionScopeTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
