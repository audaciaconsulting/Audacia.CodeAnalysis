using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionScopeForMultipleAssertions
{
    [TestClass]
    public class XunitAssertionScopeCrossFileTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionScopeForMultipleAssertionsAnalyzer();

        [TestMethod]
        public void No_Diagnostics_For_Fact_TestMethod_With_Assertion_Scope_In_Separate_File()
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
        Assert.Multiple(() =>
        {
            Assert.True(true);
            Assert.True(true);
            Assert.True(true);
        });
    }
}"
            };

            VerifyDiagnostic(sources);
        }
    }
}