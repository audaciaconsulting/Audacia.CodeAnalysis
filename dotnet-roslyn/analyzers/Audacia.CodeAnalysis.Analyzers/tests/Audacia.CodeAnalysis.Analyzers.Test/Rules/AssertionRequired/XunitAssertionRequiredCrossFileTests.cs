using Audacia.CodeAnalysis.Analyzers.Rules.AssertionRequired;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionRequired
{
    [TestClass]
    public class XunitAssertionRequiredCrossFileTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionRequiredAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Assertion_In_Separate_File()
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
        Assertion();
    }
}"
                ,
                @"
using Xunit;

namespace ConsoleApplication1;

public partial class TestClass
{
    private void Assertion()
    {
        Assert.True(true);
    }
}"
            };

            VerifyDiagnostic(sources);
        }
    }
}
