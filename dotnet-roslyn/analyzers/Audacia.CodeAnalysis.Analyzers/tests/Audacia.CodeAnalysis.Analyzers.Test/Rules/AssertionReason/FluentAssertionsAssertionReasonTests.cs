using Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionReason
{
    [TestClass]
    public class FluentAssertionsAssertionReasonTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionReasonMustBeProvidedAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Non_TestMethod()
        {
            const string testMethod = @"
public void NonTestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Assertion_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty(""Reason"");
    foo.Should().Be(""Foo"", ""Reason"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_Theory_With_Assertion_Reason()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    foo.Should().NotBeNullOrEmpty(""Reason"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Assertion_Reason_In_Separate_Method()
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

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(21, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Conditional_Assertion_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo?.Should().NotBeNullOrEmpty();
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(21, 9);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Assertion_With_And_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty(""Reason"");
    foo.Should().Be(""Foo"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Assertion_In_Separate_Method_Without_Reason()
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

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(26, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Multiple_Assertions_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
    foo.Should().Be(""Foo"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var firstExpectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(21, 5);
            var secondExpectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(22, 5);

            VerifyDiagnostic(testCode, firstExpectedDiagnostic, secondExpectedDiagnostic);
        }
    }
}
