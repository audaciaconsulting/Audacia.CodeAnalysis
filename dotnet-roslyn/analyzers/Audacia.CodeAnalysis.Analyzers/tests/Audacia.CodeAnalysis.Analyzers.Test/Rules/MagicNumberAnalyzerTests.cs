using Audacia.CodeAnalysis.Analyzers.Rules.MagicNumber;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class MagicNumberAnalyzerTests : CodeFixVerifier
    {
        private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string varName = "testVar")
        {
            return new DiagnosticResult
            {
                Id = MagicNumberAnalyzer.Id,
                Message = $"Variable declaration for '{varName}' should not use a magic number",
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
            var test = @"
namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Integer_Magic_Number_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;
    
class TypeName
{
    private int Calculate(int arg)
    {
        var testVar = 45 + arg;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 23);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Double_Magic_Number_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private double Calculate(int arg)
    {
        var testVar = 45.2 + arg;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 23);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Decimal_Magic_Number_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private decimal Calculate(int arg)
    {
        var testVar = 45.2m + arg;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 23);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Float_Magic_Number_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private float Calculate(int arg)
    {
        var testVar = 45.2f + arg;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 23);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Long_Magic_Number_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private long Calculate(int arg)
    {
        var testVar = 45l + arg;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 23);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Partial_Magic_Number_Assignment_But_Also_Const_Field()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private const int Number = 66;

    private int Calculate(int arg)
    {
        var testVar = arg + 45 - Number;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(10, 29);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Partial_Magic_Number_Assignment_But_Also_Local_Const()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int arg)
    {
        const int number = 54;
        var testVar = arg + 45 - number;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(9, 29);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Single_Integer_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate()
    {
        var testVar = 45;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Const_Field_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private const int Number = 5;

    private int Calculate(int arg)
    {
        var testVar = arg + Number;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Local_Const_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int arg)
    {
        const int number = 5;
        var testVar = arg + number;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Hardcoded_String_Assignment()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var testVar = ""Hello"";
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Integer_Magic_Number_Assignment_If_The_Magic_Number_Is_1()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int arg)
    {
        var testVar = arg + 1;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Variable_With_Double_Magic_Number_Assignment_Even_If_The_Magic_Number_Is_1()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private double Calculate(int arg)
    {
        var testVar = arg + 1.0;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 29);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Magic_Number_Assignment_If_The_Magic_Number_Is_0()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var testVar = arg ?? 0;
        return testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_Variable_With_Magic_Number_Assignment_If_The_Magic_Number_Is_10()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var testVar = arg * 10;
        return (int)testVar;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_For_Loop_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        for(int testVar = 7; testVar > 0; testVar--)
        {
            continue;
        }

        return 0;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(8, 27);
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_While_Loop_Iterable_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var count = 0;

        while(count < 11)
        {
            count++;
        }

        return count;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(10, 23, SyntaxKind.WhileStatement.ToString());
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Multi_Variable_While_Iterable_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    static void Main(string[] args)
    {
        var count = 0;

        while(count < 11 || count == 11)
        {
            count++;
        }
    }
}";
            var expected1 = BuildExpectedResult(10, 23, SyntaxKind.WhileStatement.ToString());
            var expected2 = BuildExpectedResult(10, 38, SyntaxKind.WhileStatement.ToString());
            VerifyDiagnostic(test, expected1, expected2);
        }

        [TestMethod]
        public void Diagnostic_For_If_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;

        if(check == 11)
        {
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";
            var expected = BuildExpectedResult(10, 21, SyntaxKind.IfStatement.ToString());
            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Multi_Variable_If_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;

        if(check == 11 && check != 42)
        {
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";

            var expected1 = BuildExpectedResult(10, 21, SyntaxKind.IfStatement.ToString());
            var expected2 = BuildExpectedResult(10, 36, SyntaxKind.IfStatement.ToString());
            VerifyDiagnostic(test, expected1, expected2);
        }

        [TestMethod]
        public void Diagnostic_For_Case_Switch_Label_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;

        switch (check)
        {
            case 11:
                return 0;
            default:
                return 0;
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(12, 18, SyntaxKind.CaseSwitchLabel.ToString());

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Switch_Statement_With_Magic_Number()
        {

            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;
        const int checkValue = 11;

        switch (11)
        {
            case checkValue:
                return 0;
            default:
                return 0;
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(11, 17, SyntaxKind.SwitchStatement.ToString());

            VerifyDiagnostic(test, expected);


        }

        [TestMethod]
        public void No_Diagnostic_For_Switch_Statement_With_No_Magic_Number()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;
        const int checkValue = 11;

        switch (check)
        {
            case checkValue:
                return check;
            default:
                return check;
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_While_Statement_With_No_Magic_Number()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;

        while(check == check)
        {
            return check;
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_For_Statement_With_No_Magic_Number()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;
        var iterator = 2;

        for (var counter = iterator; counter < check; counter++)
        {

        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_If_Statement_With_No_Magic_Number()
        {
            var test = @"
namespace ConsoleApplication1;

class TypeName
{
    private int Calculate(int? arg)
    {
        var check = 11;
        var iterator = 2;

        if(check != iterator)
        {
            return iterator;
        }

        return check;
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostic_For_For_Statement_With_Redeclared_Variable()
        {
            var test = @"
using System;

namespace ConsoleApplication1;

class Program
{
    private void Calculate(int? arg)
    {
        var index = 0;
        for (index = 0; index < 10; index++)
        {
            Console.WriteLine(index);
        }
    }

    static void Main(string[] args)
    {
    }
}";
            VerifyNoDiagnostic(test);
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
