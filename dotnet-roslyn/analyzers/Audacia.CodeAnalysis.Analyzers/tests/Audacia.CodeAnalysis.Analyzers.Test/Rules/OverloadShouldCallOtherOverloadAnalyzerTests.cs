using Audacia.CodeAnalysis.Analyzers.Rules.OverloadShouldCallOtherOverload;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class OverloadShouldCallOtherOverloadAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new OverloadShouldCallOtherOverloadAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string message)
        {
            return new DiagnosticResult
            {
                Id = OverloadShouldCallOtherOverloadAnalyzer.Id,
                Message = message,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_No_Method_To_Overload()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, int j)
        {
            var line = string.Format(""test"", i, j);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Should_Call_Other_Overload()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, int j)
        {
            var line = string.Format(""test"", i, j);
        }

        public virtual void TestMethod(int i, int j, int k)
        {
            var line = string.Format(""test"", i, j, k);
        }
    }
}";

            var expected = BuildExpectedResult(
                lineNumber: 6,
                column: 21,
                message: "Overloaded method 'TestClass.TestMethod(int, int)' should call another overload.");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Longer_Method_Should_Be_Virtual()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, int j)
        {
            TestMethod(i, j, 0);
        }

        public void TestMethod(int i, int j, int k)
        {
            var line = string.Format(""test"", i, j, k);
        }
    }
}";

            var expected = BuildExpectedResult(
                    lineNumber: 11,
                    column: 21,
                    message: "Method overload with the most parameters should be virtual.");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Does_Not_Match_Parameter_Order_Of_Longest_Overload()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public virtual void TestMethod(int i, string s, int j = 0)
        {
            var line = string.Format(s, i, j);
        }
    }

    public class TestClassA : TestClass
    {
        public void TestMethod(string s, int i)
        {
            base.TestMethod(i, s, 0);
        }
    }
}";

            var expected = BuildExpectedResult(
                lineNumber: 14,
                column: 21,
                message: "Parameter order in 'TestClassA.TestMethod(string, int)' does not match with the parameter order of the longest overload.");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Failing_All_Three_Rules()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, int j)
        {
            TestMethod(i, j, 0);
        }

        public void TestMethod(int i, string s, int j = 0)
        {
            var line = string.Format(s, i, j);
        }
    }

    public class TestClassA : TestClass
    {
        public void TestMethod(string s, int i)
        {
            base.TestMethod(i, s, 0);
        }
    }
}";
            var expectedList = new[]
            {
                BuildExpectedResult(lineNumber: 6, column: 21, message: "Overloaded method 'TestClass.TestMethod(int, int)' should call another overload."),
                BuildExpectedResult(lineNumber: 11, column: 21, message: "Method overload with the most parameters should be virtual."),
                BuildExpectedResult(lineNumber: 19, column: 21, message: "Parameter order in 'TestClassA.TestMethod(string, int)' does not match with the parameter order of the longest overload.")
            };

            VerifyDiagnostic(test, expectedList);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Overload_Correctly()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public virtual void TestMethod(int i, string s, int j = null)
        {
            var line = string.Format(s, i, j);
        }
    }

    public class TestClassA : TestClass
    {
        public override void TestMethod(int i, string s, int j)
        {
            base.TestMethod(i, s);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Overload_Correctly_Without_Inheritance()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, string s)
        {
            TestMethod(i, s, 0);
        }

        public virtual void TestMethod(int i, string s, int j = 0)
        {
            var line = string.Format(s, i, j);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_Method_Should_Not_Overload_With_Get_Attributes()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClassController
    {
        [HttpGet]
        public void Get(int i)
        {
            var line = string.Format(""Test"", i);
        }

        [HttpGet(""Test"")]
        public void Get(int i, string s)
        {
            var line = string.Format(s, i);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_Method_Should_Not_Overload_With_Put_Attributes()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClassController : Controller
    {
        [HttpPut]
        public void Get(int i)
        {
            var line = string.Format(""Test"", i);
        }

        [HttpPut(""Test"")]
        public void Get(int i, string s)
        {
            var line = string.Format(s, i);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_Method_Should_Not_Overload()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClassController : Controller
    {
        public void Get(int i)
        {
            var line = string.Format(""Test"", i);
        }

        public void Get(int i, string s)
        {
            var line = string.Format(s, i);
        }
    }
}";

            VerifyNoDiagnostic(test);
        }
    }
}
