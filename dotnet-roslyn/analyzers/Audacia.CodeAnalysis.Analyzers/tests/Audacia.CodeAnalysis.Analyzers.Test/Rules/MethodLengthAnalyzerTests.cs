using Audacia.CodeAnalysis.Analyzers.Rules.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class MethodLengthAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MethodLengthAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult(string memberName, int lineNumber, int column, int statementCount, int maxStatementCount = MethodLengthAnalyzer.DefaultMaxStatementCount)
        {
            return new DiagnosticResult
            {
                Id = MethodLengthAnalyzer.Id,
                Message = $"Method '{memberName}' contains {statementCount} statements, which exceeds the maximum of {maxStatementCount} statements.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Less_Than_Max_Allowed_Statements()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var lineOne = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Body_Greater_Than_Max_Allowed_Statements()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
            var lineEleven = ""Hello"";
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "TestClass.TestMethod()",
                lineNumber: 6,
                column: 21,
                statementCount: 11);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Constructor_Body_Greater_Than_Max_Allowed_Statements()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public TestClass()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
            var lineEleven = ""Hello"";
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "TestClass.TestClass()",
                lineNumber: 6,
                column: 16,
                statementCount: 11);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Excluding_Whitespace()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";

            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";

            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Excluding_One_Line_Argument_Null_Checks()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Excluding_Multi_Line_Argument_Null_Checks()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        // In the next four tests, MaxMethodLengthAttribute is declared inline in the test code rather than using the actual type
        // in the Audacia.CodeAnalysis.Analyzers library
        // There is an open issue about this: https://github.com/dotnet/roslyn/issues/30248

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxMethodLength(12)]
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
            var lineEleven = ""Hello"";
            var lineTwelve = ""Hello"";
        }
    }

    public sealed class MaxMethodLengthAttribute : System.Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Overridden_Via_Full_Name_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [MaxMethodLengthAttribute(12)]
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
            var lineEleven = ""Hello"";
            var lineTwelve = ""Hello"";
        }
    }

    public sealed class MaxMethodLengthAttribute : System.Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Overridden_Via_Fully_Qualified_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [TestNamespace.MaxMethodLengthAttribute(12)]
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
            var lineEleven = ""Hello"";
            var lineTwelve = ""Hello"";
        }
    }

    public sealed class MaxMethodLengthAttribute : System.Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_Greater_Than_Max_Allowed_Statements_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        [TestNamespace.MaxMethodLengthAttribute(3)]
        public void TestMethod()
        {
            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
        }
    }

    public sealed class MaxMethodLengthAttribute : System.Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "TestClass.TestMethod()",
                lineNumber: 7,
                column: 21,
                statementCount: 4,
                maxStatementCount: 3);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Equal_To_Max_Allowed_Statements_Excluding_Argument_Null_Exception_Throw_If_Null()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Body_Equal_To_Max_Allowed_Statements_Including_Argument_Null_Exception()
        {
            var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(string value)
        {
            ArgumentNullException.ReferenceEquals(value, value);

            var lineOne = ""Hello"";
            var lineTwo = ""Hello"";
            var lineThree = ""Hello"";
            var lineFour = ""Hello"";
            var lineFive = ""Hello"";
            var lineSix = ""Hello"";
            var lineSeven = ""Hello"";
            var lineEight = ""Hello"";
            var lineNine = ""Hello"";
            var lineTen = ""Hello"";
        }
    }
}";

            var expected = BuildExpectedResult(
                memberName: "TestClass.TestMethod(string)",
                lineNumber: 6,
                column: 21,
                statementCount: 11);

            VerifyDiagnostic(test, expected);
        }
    }
}
