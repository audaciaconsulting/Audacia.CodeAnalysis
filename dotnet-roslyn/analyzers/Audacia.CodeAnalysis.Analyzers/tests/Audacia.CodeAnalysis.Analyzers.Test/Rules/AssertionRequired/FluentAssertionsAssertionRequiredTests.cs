using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class FluentAssertionsAssertionRequiredTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Single_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Theory_TestMethod_With_Single_Assertion()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    foo.Should().NotBeNullOrEmpty();
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Conditional_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    foo?.Should().NotBeEmpty();
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Assertion_In_Separate_Method()
        {
            const string testMethod = @"
private void Assertion(string foo)
{
    foo.Should().NotBeNullOrEmpty();
    foo.Should().Be(""Foo"");
}

[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assertion(foo);
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Theory_TestMethod_With_Assertion_In_Separate_Method()
        {
            const string testMethod = @"
private void Assertion(string foo)
{
    foo.Should().NotBeNullOrEmpty();
    foo.Should().Be(""Foo"");
}

[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    Assertion(foo);
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Assertion_Inside_FluentAssertions_Assertion_Scope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using (new AssertionScope())
    {
        foo.Should().NotBeEmpty();
    }
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Assertion_Inside_Inline_FluentAssertions_Assertion_Scope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using var _ = new AssertionScope();
    foo.Should().NotBeEmpty();
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Inline_Assertion_Scope_Without_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using var _ = new AssertionScope();
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionRequiredTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Block_Assertion_Scope_Without_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    using (new AssertionScope())
    {
    }
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionRequiredTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
