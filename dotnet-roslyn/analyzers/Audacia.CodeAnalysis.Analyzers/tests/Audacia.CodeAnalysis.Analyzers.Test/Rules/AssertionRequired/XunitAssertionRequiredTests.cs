using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class XunitAssertionRequiredTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.NotEmpty(foo);
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Theory_TestMethod_With_Assertion()
        {
            const string testMethod = @"
[Theory]
[InlineData(""Foo"")]
public void TestMethod(string foo)
{
    Assert.NotEmpty(foo);
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
    Assert.NotEmpty(foo);
    Assert.Equal(""Foo"", foo);
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
    Assert.NotEmpty(foo);
    Assert.Equal(""Foo"", foo);
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
        public void No_Diagnostics_For_Assertion_Inside_Xunit_Assertion_Scope()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Multiple(() => {
        Assert.NotEmpty(foo);
    });
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Assertion_Scope_Without_Assertion()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Multiple(() => {
        
    });
}";

            var testCode = BaseAssertionRequiredTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionRequiredTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
