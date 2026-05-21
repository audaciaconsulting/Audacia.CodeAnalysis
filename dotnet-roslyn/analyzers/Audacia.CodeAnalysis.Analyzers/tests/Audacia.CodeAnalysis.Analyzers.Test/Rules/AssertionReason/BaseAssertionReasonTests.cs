using Audacia.CodeAnalysis.Analyzers.Rules.AssertionReason;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules.AssertionReason
{
    [TestClass]
    public class BaseAssertionReasonTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AssertionReasonMustBeProvidedAnalyzer();

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
            return new DiagnosticResult
            {
                Id = AssertionReasonMustBeProvidedAnalyzer.Id,
                Severity = DiagnosticSeverity.Warning,
                Message = "Assertions must contain a reason",
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            var testCode = BuildTestCode("");

            VerifyNoDiagnostic(testCode);
        }
    }
}
