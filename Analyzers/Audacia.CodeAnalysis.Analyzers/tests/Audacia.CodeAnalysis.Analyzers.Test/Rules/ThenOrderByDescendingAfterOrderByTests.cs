using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.ThenOrderByDescendingAfterOrderBy;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ThenOrderByDescendingAfterOrderByTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ThenOrderByDescendingAfterOrderByAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult()
        {
            return new DiagnosticResult
            {
                
            };
        }

        [TestMethod]
        public void Debugging_Test()
        {
            var test = @"
            namespace ConsoleApplication1
            {
                class TypeName
                {
                    private int number;
                }
            }";

            VerifyDiagnostic(test);
        }

    }
}
