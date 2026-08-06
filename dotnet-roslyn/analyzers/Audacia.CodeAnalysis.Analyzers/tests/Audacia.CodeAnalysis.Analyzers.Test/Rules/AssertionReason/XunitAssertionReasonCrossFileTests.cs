using Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionReason
{
    [TestClass]
    public class XunitAssertionReasonCrossFileTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionReasonMustBeProvidedAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Assertion_Reason_In_Separate_File()
        {
            var sources = new[]
            {
                @"
using Xunit;

namespace ConsoleApplication1;

public partial class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void TestMethod()
    {
        Assertion(true);
    }
}"
                ,
                @"
using Xunit;

namespace ConsoleApplication1;

public partial class TestClass
{
    private void Assertion(bool value)
    {
        Assert.True(value, ""Reason"");
    }
}"
            };

            VerifyDiagnostic(sources);
        }
    }
}