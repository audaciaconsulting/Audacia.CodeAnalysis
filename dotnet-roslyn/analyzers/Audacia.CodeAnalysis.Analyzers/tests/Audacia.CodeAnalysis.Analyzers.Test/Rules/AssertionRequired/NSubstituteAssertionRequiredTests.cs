using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class NSubstituteAssertionRequiredTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        private static string BuildTestCode(string testMethod)
            => BaseAssertionRequiredTests.BuildTestCode(testMethod);

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_Received_Verification()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    substitute.Bar();
    NSubstitute.SubstituteExtensions.Received(substitute, 1).Bar();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_ReceivedWithAnyArgs_Verification()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    NSubstitute.SubstituteExtensions.ReceivedWithAnyArgs(substitute).Bar();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_DidNotReceive_Verification()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    NSubstitute.SubstituteExtensions.DidNotReceive(substitute).Bar();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_DidNotReceiveWithAnyArgs_Verification()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    NSubstitute.SubstituteExtensions.DidNotReceiveWithAnyArgs(substitute).Bar();
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_InOrder_Verification()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    NSubstitute.Received.InOrder(() => substitute.Bar());
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_NSubstitute_Received_Verification_In_Helper_Method()
        {
            const string testMethod = @"
private interface IFoo
{
    void Bar();
}

private static void VerifyReceived(IFoo substitute)
{
    NSubstitute.SubstituteExtensions.Received(substitute, 1).Bar();
}

[Fact]
public void TestMethod()
{
    var substitute = NSubstitute.Substitute.For<IFoo>();
    substitute.Bar();
    VerifyReceived(substitute);
}";

            var testCode = BuildTestCode(testMethod);

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Non_NSubstitute_Received_Method()
        {
            const string testMethod = @"
[Fact]
public void TestMethod()
{
    Received();
}

private static void Received()
{
}";

            var testCode = BuildTestCode(testMethod);
            var expectedDiagnostic = BaseAssertionRequiredTests.BuildExpectedResult(18, 13);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}
