using Audacia.CodeAnalysis.Analyzers.Rules.AssertionScopeForMultipleAssertions;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionScopeForMultipleAssertions
{
    [TestClass]
    public class BaseAssertionScopeTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new AssertionScopeForMultipleAssertionsAnalyzer();

        internal static string BuildTestCode(string testMethod)
        {
            return @"
using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Shouldly;
using Xunit;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }

    " + testMethod + @"
}";
        }

        internal static DiagnosticResult BuildExpectedResult(int lineNumber, int column)
        {

            var diagnosticResult = new DiagnosticResult
            {
                Id = AssertionScopeForMultipleAssertionsAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = "Use an assertion scope when a test method has more than two assertions",
                Locations =
                    new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
        }

        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            var testCode = BuildTestCode("");

            VerifyNoDiagnostic(testCode);
        }
    }
}
