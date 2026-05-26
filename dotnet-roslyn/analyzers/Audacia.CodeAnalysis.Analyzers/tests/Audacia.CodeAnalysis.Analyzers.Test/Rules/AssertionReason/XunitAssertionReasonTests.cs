using Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionReason
{
    [TestClass]
    public class XunitAssertionReasonTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionReasonMustBeProvidedAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Non_TestMethod()
        {
            const string testMethod = @"
public void NonTestMethod()
{
    Assert.NotEmpty(""foo"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

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
        public void No_Diagnostics_For_TestMethod_With_Assertion_Reason(string attributeName)
        {
            var testMethod = @"
[" + attributeName + @"]
public void TestMethod()
{
    var foo = true;
    Assert.False(foo, ""Reason"");
    Assert.True(foo, ""Reason"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_TestMethod_With_Assertion_Without_Required_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = ""Foo"";
    Assert.Equal(""Foo"", foo);
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

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
    var foo = true;
    Assertion(foo);
}

private void Assertion(bool foo)
{
    Assert.True(foo, ""Reason"");
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);

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
        public void Diagnostics_For_Assertion_Without_Reason(string attributeName)
        {
            var testMethod = @"
[" + attributeName + @"]
public void TestMethod()
{
    var foo = true;
    Assert.True(foo);
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(21, 5);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Assertion_With_And_Without_Reason()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    var foo = true;
    Assert.False(foo, ""Reason"");
    Assert.True(foo);
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
    var foo = true;
    Assertion(foo);
}

private void Assertion(bool foo)
{
    Assert.True(foo);
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
    var foo = true;
    Assert.False(foo);
    Assert.True(foo);
    Assert.Fail();
}";

            var testCode = BaseAssertionReasonTests.BuildTestCode(testMethod);
            var firstExpectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(21, 5);
            var secondExpectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(22, 5);
            var thirdExpectedDiagnostic = BaseAssertionReasonTests.BuildExpectedResult(23, 5);

            VerifyDiagnostic(testCode, firstExpectedDiagnostic, secondExpectedDiagnostic, thirdExpectedDiagnostic);
        }
    }
}
