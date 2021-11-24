using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.FieldWithUnderscore;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class FieldWithUnderscoreAnalyzerTests : CodeFixVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {
            return new DiagnosticResult
            {
                Id = FieldWithUnderscoreAnalyzer.Id,
                Message = "Field 'number' is not prefixed with an underscore.",
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
        public void Diagnostic_And_Code_Fix_For_Private_Field_In_A_Class()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int number;
        }
    }";
            var expected = BuildExpectedResult(6, 25);

            VerifyDiagnostic(test, expected);

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int _number;
        }
    }";
            VerifyCodeFix(test, fixtest);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Private_Field_In_A_Struct()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int number;
        }
    }";
            var expected = BuildExpectedResult(6, 25);

            VerifyDiagnostic(test, expected);

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int _number;
        }
    }";
            VerifyCodeFix(test, fixtest);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Private_Readonly_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int number;
        }
    }";
            var expected = BuildExpectedResult(6, 34);

            VerifyDiagnostic(test, expected);

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int _number;
        }
    }";
            VerifyCodeFix(test, fixtest);
        }

        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Private_Static_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int number;
        }
    }";
            var expected = BuildExpectedResult(6, 32);

            VerifyDiagnostic(test, expected);

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int _number;
        }
    }";
            VerifyCodeFix(test, fixtest);
        }

        [TestMethod]
        public void No_Diagnostic_For_Public_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Number;
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Internal_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            internal int Number;
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Private_Const_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private const int Number = 4;
        }
    }";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Private_Static_Readonly_Field()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static readonly int Number = 5;
        }
    }";
            VerifyNoDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new FieldWithUnderscoreCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new FieldWithUnderscoreAnalyzer();
        }
    }
}
