﻿using Audacia.CodeAnalysis.Analyzers.Rules.IncludeBraces;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class IncludeBracesAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new IncludeBracesAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {
            return new DiagnosticResult
            {
                Id = IncludeBracesAnalyzer.Id,
                Message = "Code block should have braces",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_If_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        if (arg == 0)
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Else_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        if (arg == 0)
        {

        }
        else
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Foreach_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int[] args)
    {
        foreach (var item in args)
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_For_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        for (var i = 0; i < arg; i++)
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_While_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        var count = 0;
        while (count < arg)
        {
            count++;
        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Do_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        var count = 0;
        do
        {
            count++;
        } while (count < arg);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Using_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod()
    {
        using (var stream = new System.IO.MemoryStream())
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Lock_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    private readonly object _lockToken = new object();

    public void TestMethod(int arg)
    {
        lock (_lockToken)
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void No_Diagnostics_For_Fixed_Statement_Containing_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    unsafe static void TestMethod(string arg)
    {
        fixed (char* pointer = arg)
        {

        }
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostic_For_If_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        if (arg == 0)
            Console.WriteLine(arg);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(11, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Else_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        if (arg == 0)
        {

        }
        else
            Console.WriteLine(arg);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(15, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Foreach_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int[] args)
    {
        foreach (var item in args)
            Console.WriteLine(item);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(11, 13));
        }

        [TestMethod]
        public void Diagnostic_For_For_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        for (var i = 0; i < arg; i++)
            Console.WriteLine(arg);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(11, 13));
        }

        [TestMethod]
        public void Diagnostic_For_While_Statement_With_No_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        var count = 0;
        while (count < arg)
            count++;
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(10, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Do_Statement_With_No_Braces()
        {
            var test = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod(int arg)
    {
        var count = 0;
        do
            count++;
        while (count < arg);
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(10, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Using_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    static void Main(string[] args)
    {
        using (var stream = new System.IO.MemoryStream())
            Console.WriteLine();
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(11, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Lock_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    private readonly object _lockToken = new object();

    public void TestMethod(int arg)
    {
        lock (_lockToken)
            Console.WriteLine();
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(13, 13));
        }

        [TestMethod]
        public void Diagnostic_For_Fixed_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    unsafe static void TestMethod(string arg)
    {
        fixed (char* pointer = arg)
            Console.WriteLine();
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyDiagnostic(test, BuildExpectedResult(11, 13));
        }

        [TestMethod]
        public void No_Diagnostics_For_Argument_Null_Check_If_Statement_With_No_Braces()
        {
            var test = @"
using System;

namespace TestNamespace;

public class TestClass
{
    public void TestMethod(string arg)
    {
        if (arg == null) throw new ArgumentNullException(nameof(arg));
    }

    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(test);
        }
    }
}