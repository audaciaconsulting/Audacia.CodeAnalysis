using Audacia.CodeAnalysis.Analyzers.Rules.MagicNumber;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class MagicNumberAnalyzerTests : CodeFixVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {
            return new DiagnosticResult
            {
                Id = MagicNumberAnalyzer.Id,
                Message = "Variable declaration for 'testVar' should not use a magic number.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            var test = @"";
            
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Integer_Magic_Number_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = 45 + arg;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 31);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Double_Magic_Number_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = 45.2 + arg;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 31);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Decimal_Magic_Number_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = 45.2m + arg;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 31);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Float_Magic_Number_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = 45.2f + arg;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 31);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Long_Magic_Number_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = 45l + arg;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 31);

            VerifyDiagnostic(test, expected);
        }
        
        [TestMethod]
        public void Diagnostic_For_Variable_With_Partial_Magic_Number_Assignment_But_Also_Const_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private const int Number = 66;

            private int Calculate(int arg)
            {
                var testVar = arg + 45 - Number;
            }
        }
    }";
            var expected = BuildExpectedResult(10, 37);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Partial_Magic_Number_Assignment_But_Also_Local_Const()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                const int number = 54;
                var testVar = arg + 45 - number;
            }
        }
    }";
            var expected = BuildExpectedResult(9, 37);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Single_Integer_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate()
            {
                var testVar = 45;
            }
        }
    }";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Const_Field_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private const int Number = 5;

            private int Calculate(int arg)
            {
                var testVar = arg + Number;
            }
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Local_Const_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                const int number = 5;
                var testVar = arg + number;
            }
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Hardcoded_String_Assignment()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private void DoStuff()
            {
                var testVar = ""Hello"";
            }
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Integer_Magic_Number_Assignment_If_The_Magic_Number_Is_1()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = arg + 1;
            }
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Double_Magic_Number_Assignment_Even_If_The_Magic_Number_Is_1()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int Calculate(int arg)
            {
                var testVar = arg + 1.0;
            }
        }
    }";
            var expected = BuildExpectedResult(8, 37);

            VerifyDiagnostic(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MagicNumberAnalyzer();
        }
    }
}
