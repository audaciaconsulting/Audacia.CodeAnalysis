using Audacia.CodeAnalysis.Analyzers.TestProjects.Rules.AssertionReason;
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

        [TestMethod]
        public void No_Crash_For_Fact_TestMethod_Calling_Helper_In_Referenced_Project()
        {
            var sources = new[]
            {
                @"
using Helpers;
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    [Fact]
    public void TestMethod()
    {
        AssertionHelpers.Assertion(true);
    }
}"
            };

            // The helper lives in a referenced project, so its syntax tree is not part of the test project's compilation.
            // This previously threw "SyntaxTree is not part of the compilation" in the IDE.
            var referencedSources = new[]
            {
                @"
using Xunit;

namespace Helpers;

public static class AssertionHelpers
{
    public static void Assertion(bool value)
    {
        Assert.True(value, ""Reason"");
    }
}"
            };

            VerifyDiagnosticWithProjectReference(sources, referencedSources);
        }
    }
}
