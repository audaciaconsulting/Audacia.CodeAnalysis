using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class MoqAssertionRequiredTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        private static string BuildTestCode(string testMethod)
            => BaseAssertionRequiredTests.BuildTestCode(testMethod);

        [TestMethod]
        public void No_Diagnostics_For_Moq_Verify()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var mock = new Moq.Mock<IFoo>();
    mock.Object.Bar();
    mock.Verify(foo => foo.Bar(), Moq.Times.Once());
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Moq_VerifyNoOtherCalls()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var mock = new Moq.Mock<IFoo>();
    mock.Object.Bar();
    mock.Verify(foo => foo.Bar(), Moq.Times.Once());
    mock.VerifyNoOtherCalls();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Moq_Verification_In_Helper_Method()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

private void VerifyInvocation(Moq.Mock<IFoo> mock)
{
    mock.Verify(foo => foo.Bar(), Moq.Times.Once());
}

[Fact]
public void TestMethod()
{
    var mock = new Moq.Mock<IFoo>();
    mock.Object.Bar();
    VerifyInvocation(mock);
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Non_Moq_Verify_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    Verify();
}

private static void Verify()
{
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionRequiredTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
