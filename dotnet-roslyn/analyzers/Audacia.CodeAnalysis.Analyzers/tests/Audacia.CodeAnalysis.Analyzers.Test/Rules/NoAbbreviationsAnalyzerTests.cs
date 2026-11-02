using Audacia.CodeAnalysis.Analyzers.Rules.NoAbbreviations;
using Audacia.CodeAnalysis.Analyzers.Shared.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class NoAbbreviationsAnalyzerTests : DiagnosticVerifier
    {
        private readonly Mock<ISettingsReader> _mockSettingsReader = new();

        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string kind, string abbreviationUsed)
        {
            return new DiagnosticResult
            {
                Id = NoAbbreviationsAnalyzer.Id,
                Message = $"{kind} '{abbreviationUsed}' should have a more descriptive name",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoAbbreviationsAnalyzer(_mockSettingsReader.Object);
        }

        [TestMethod]
        public void No_Diagnostic_If_No_Abbreviation_Used()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var noAbbreviation = 1.0;
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Variable_Name_Is_Used()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var d = 1.0;
    }
}";

            var expected = BuildExpectedResult(8, 13, "Variable", "d");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_If_A_Disallowed_Abbreviation_Variable_Name_Is_Used()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var len = 1.0;
    }
}";

            var expected = BuildExpectedResult(8, 13, "Variable", "len");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Method_Parameter_Name_Is_Used()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private void MethodName(string s)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(6, 36, "Parameter", "s");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Lambda_Expression_Parameter_Name_Is_Used()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var result = args.Where(x => x.Length == 0);
    }
}";

            var expected = BuildExpectedResult(10, 33, "Parameter", "x");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Type_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var result = args.Where(s => s.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Queried_Collection_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = arguments.Where(a => a.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Null_Conditional_Collection_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = arguments?.Where(a => a.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Lambda_Parameter_Does_Not_Match_Null_Conditional_Collection_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = arguments?.Where(x => x.Length == 0);
    }
}";

            var expected = BuildExpectedResult(11, 39, "Parameter", "x");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Static_Extension_Call_Collection_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = Enumerable.Where(arguments, a => a.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Static_Class_Name_Instead_Of_Collection()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = Enumerable.Where(arguments, e => e.Length == 0);
    }
}";

            var expected = BuildExpectedResult(11, 50, "Parameter", "e");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Anonymous_Method_Parameter_Matches_Collection_Name()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = arguments.Where(delegate(string a) { return a.Length == 0; });
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Anonymous_Method_Parameter_Only_Matches_Outer_Collection_Name()
        {
            var test = @"
using System;
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = arguments.Select(argument => (Func<int, bool>)delegate(int a) { return a == 0; });
    }
}";

            var expected = BuildExpectedResult(12, 81, "Parameter", "a");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Wrapped_Lambda_Parameter_Matches_Collection_Name()
        {
            var test = @"
using System;
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var parenthesized = arguments.Where((a => a.Length == 0));
        var cast = arguments.Where((Func<string, bool>)(a => a.Length == 0));
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Single_Character_Wrapped_Lambda_Parameter_Does_Not_Match_Collection_Name()
        {
            var test = @"
using System;
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var cast = arguments.Where((Func<string, bool>)(x => x.Length == 0));
    }
}";

            var expected = BuildExpectedResult(12, 57, "Parameter", "x");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Single_Character_Lambda_Parameter_Matches_Static_Non_Extension_Call_Collection_Name()
        {
            var test = @"
using System;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var arguments = args;
        var result = Array.Find(arguments, a => a.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_If_A_Longer_Lambda_Parameter_Name_Is_Used()
        {
            var test = @"
using System.Linq;

namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var result = args.Where(arg => arg.Length == 0);
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_If_A_Loop_Variable_Abbreviation_Not_In_The_Allowed_List_Is_Used()
        {
            _mockSettingsReader.Setup(settings => settings.TryGetValue(
                    It.IsAny<SyntaxTree>(),
                    new SettingsKey(NoAbbreviationsAnalyzer.Id, NoAbbreviationsAnalyzer.AllowedLoopVariablesSetting)))
                .Returns("i");

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        for (var a = 0; a < args.Length; a++)
        {
        }
    }
}";

            var expected = BuildExpectedResult(8, 18, "Variable", "a");

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_If_An_Allowed_Loop_Variable_Abbreviation_Is_Used()
        {
            _mockSettingsReader.Setup(settings => settings.TryGetValue(
                    It.IsAny<SyntaxTree>(),
                    new SettingsKey(NoAbbreviationsAnalyzer.Id, NoAbbreviationsAnalyzer.AllowedLoopVariablesSetting)))
                .Returns("i,j");

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
        }
    }
}";

            VerifyNoDiagnostic(test);
        }
    }
}
