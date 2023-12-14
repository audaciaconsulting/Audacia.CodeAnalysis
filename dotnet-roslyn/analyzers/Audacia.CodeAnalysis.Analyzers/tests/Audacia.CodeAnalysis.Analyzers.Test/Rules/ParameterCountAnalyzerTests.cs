using Audacia.CodeAnalysis.Analyzers.Rules.ParameterCount;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ParameterCountAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ParameterCountAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult(string memberName, int lineNumber, int column, int parameterCount,
            int maxParameterCount = ParameterCountAnalyzer.DefaultMaxParameterCount)
        {
            return new DiagnosticResult
            {
                Id = ParameterCountAnalyzer.Id,
                Message =
                    $"{memberName} contains {parameterCount} parameters, which exceeds the maximum of {maxParameterCount} parameters.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int i, int j)
        {
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_One_More_Than_Max_Allowed_Number_But_Last_Is_Excluded_Type()
        {
            var test = @"namespace TestNamespace
                        {
                            public class TestClass
                            {
                                public void TestMethod(int i, int j, int k, int l, CancellationToken token)
                                {
                                }
                            }
                        }";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_One_More_Than_Max_Allowed_Number_But_Has_Excluded_Type()
        {
            var test = @"namespace TestNamespace
                        {
                            public class TestClass
                            {
                                public void TestMethod(int i, int j, int k, CancellationToken token, int l)
                                {
                                }
                            }
                        }";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Two_More_Than_Max_Allowed_Number_But_Last_Is_Excluded_Type()
        {
            var test = @"namespace TestNamespace
                        {
                            public class TestClass
                            {
                                public void TestMethod(int i, int j, int k, int l, int p, CancellationToken token)
                                {
                                }
                            }
                        }";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 5,
                column: 45,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Two_More_Than_Max_Allowed_Number_But_Has_Excluded_Type()
        {
            var test = @"namespace TestNamespace
                        {
                            public class TestClass
                            {
                                public void TestMethod(int i, int j, int k, int l, CancellationToken token, int p)
                                {
                                }
                            }
                        }";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 5,
                column: 45,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Constructor_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public TestClass(int i, int j)
        {
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int a, int b, int c, int d)
        {
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Constructor_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public TestClass(int a, int b, int c, int d)
        {
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Parameters_More_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(int a, int b, int c, int d, int e)
        {
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 6,
                column: 21,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Constructor_Parameters_More_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public TestClass(int a, int b, int c, int d, int e)
        {
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 6,
                column: 16,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxParameterCount(5)]
        public void TestMethod(int a, int b, int c, int d, int e)
        {
        }
    }

    public sealed class MaxParameterCountAttribute : System.Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Constructor_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxParameterCount(5)]
        public TestClass(int a, int b, int c, int d, int e)
        {
        }
    }

    public sealed class MaxParameterCountAttribute : System.Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxParameterCountAttribute(1)]
        public void TestMethod(int a, int b)
        {
        }
    }

    public sealed class MaxParameterCountAttribute : System.Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 7,
                column: 21,
                parameterCount: 2,
                maxParameterCount: 1);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Constructor_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxParameterCountAttribute(1)]
        public TestClass(int a, int b)
        {
        }
    }

    public sealed class MaxParameterCountAttribute : System.Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 7,
                column: 16,
                parameterCount: 2,
                maxParameterCount: 1);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Record_Constructor_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace
{
    public record TestClass (int a, int b, int c, int d)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Record_Constructor_Parameters_Greater_Than_Max_Allowed()
        {
            var test = @"
namespace TestNamespace
{
    public record TestClass (int a, int b, int c, int d, int e, int f)
    {
    }
}";
            VerifyDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Record_Constructor_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    [MaxParameterCountAttribute(1)]
    public record TestClass (int a, int b, int c, int d, int e, int f)
    {
    }

    public sealed class MaxParameterCountAttribute : System.Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}";
            VerifyDiagnostic(test);
        }
    }
}
