﻿using Audacia.CodeAnalysis.Analyzers.Rules.ParameterCount;
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
                    $"{memberName} contains {parameterCount} parameters, which exceeds the maximum of {maxParameterCount} parameters",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int i, int j)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_One_More_Than_Max_Allowed_Number_But_Last_Is_Excluded_Type()
        {
            var test = @"
using System.Threading;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int i, int j, int k, int l, CancellationToken token)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_One_More_Than_Max_Allowed_Number_But_Has_Excluded_Type()
        {
            var test = @"
using System.Threading;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int i, int j, int k, CancellationToken token, int l)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Two_More_Than_Max_Allowed_Number_But_Last_Is_Excluded_Type()
        {
            var test = @"
using System.Threading;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int i, int j, int k, int l, int p, CancellationToken token)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 8,
                column: 17,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Two_More_Than_Max_Allowed_Number_But_Has_Excluded_Type()
        {
            var test = @"
using System.Threading;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int i, int j, int k, int l, CancellationToken token, int p)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 8,
                column: 17,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Constructor_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public TestClass(int i, int j)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int a, int b, int c, int d)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Constructor_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public TestClass(int a, int b, int c, int d)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Parameters_More_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int a, int b, int c, int d, int e)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 6,
                column: 17,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Class_Constructor_Parameters_More_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public TestClass(int a, int b, int c, int d, int e)
    {
    }

    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 6,
                column: 12,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Class_Primary_Constructor_Parameters_More_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass(int a, int b, int c, int d, int e);

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 4,
                column: 14,
                parameterCount: 5);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    [MaxParameterCount(5)]
    public void TestMethod(int a, int b, int c, int d, int e)
    {
    }

    static void Main(string[] args)
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
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Constructor_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    [MaxParameterCount(5)]
    public TestClass(int a, int b, int c, int d, int e)
    {
    }

    static void Main(string[] args)
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
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Primary_Constructor_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

[MaxParameterCount(5)]
public class TestClass(int a, int b, int c, int d, int e);

public class EntryClass()
{
    static void Main(string[] args)
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
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Primary_Constructor_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass(int a, int b, int c, int d);

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Class_Primary_Constructor_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public class TestClass(int i, int j);

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    [MaxParameterCountAttribute(1)]
    public void TestMethod(int a, int b)
    {
    }

    static void Main(string[] args)
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
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'TestMethod'",
                lineNumber: 7,
                column: 17,
                parameterCount: 2,
                maxParameterCount: 1);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Class_Constructor_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    [MaxParameterCountAttribute(1)]
    public TestClass(int a, int b)
    {
    }

    static void Main(string[] args)
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
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 7,
                column: 12,
                parameterCount: 2,
                maxParameterCount: 1);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Class_Primary_Constructor_Parameters_Greater_Than_Max_Allowed_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

[MaxParameterCountAttribute(1)]
public class TestClass(int a, int b);

public sealed class MaxParameterCountAttribute : System.Attribute
{
    public int ParameterCount { get; }

    public MaxParameterCountAttribute(int parameterCount)
    {
        ParameterCount = parameterCount;
    }
}

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Constructor for 'TestClass'",
                lineNumber: 5,
                column: 14,
                parameterCount: 2,
                maxParameterCount: 1);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void No_Diagnostics_For_Record_Primary_Constructor_Parameters_Equal_To_Max_Allowed_Number_Overridden_Via_Attribute()
        {
            var test = @"
namespace TestNamespace;

[MaxParameterCount(5)]
public record TestRecord(int a, int b, int c, int d, int e);

public sealed class MaxParameterCountAttribute : System.Attribute
{
    public int ParameterCount { get; }

    public MaxParameterCountAttribute(int parameterCount)
    {
        ParameterCount = parameterCount;
    }
}

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Record_Primary_Constructor_Parameters_Equal_To_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public record TestRecord(int a, int b, int c, int d);

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Record_Primary_Constructor_Parameters_Less_Than_Max_Allowed_Number()
        {
            var test = @"
namespace TestNamespace;

public record TestRecord(int i, int j);

public class EntryClass()
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }
        
         [TestMethod]
         public void No_Diagnostics_For_Method_Parameters_Equal_To_Max_Allowed_Number_With_This_Param()
         {
             var test = @"
namespace TestNamespace;

public class TestClass
{
    public void ExampleFunction()
    {
    }

    static void Main(string[] args)
    {
    }
}

public static class TestClassExtensions
{
    public static string Extension(this TestClass testClass, int a, int b, int c, int d)
    {
    
    return string.Empty;
    }
}";
             VerifyNoDiagnostic(test);
         }

        [TestMethod]
        public void Diagnostics_For_Method_Parameters_Greater_To_Max_Allowed_Number_With_This_Param()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void ExampleFunction()
    {
    }

    static void Main(string[] args)
    {
    }
}

public static class TestClassExtensions
{
    public static string Extension(this TestClass testClass, int a, int b, int c, int d, int e)
    {
        return string.Empty;
    }
}";

            var expected = BuildExpectedResult(
                memberName: "Method 'Extension'",
                lineNumber: 17,
                column: 26,
                parameterCount: 5,
                maxParameterCount: 4);

            VerifyDiagnostic(test, expected);
        }
    }
}