using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Rules.NullableReferenceTypes;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    /// <summary>
    /// Unit tests for the <see cref="NullableReferenceTypesAnalyzer"/> and the <see cref="NullableReferenceTypesCodeFixProvider"/>.
    /// </summary>
    [TestClass]
    public class NullableReferenceTypesTests : CodeFixVerifier
    {
        /// <summary>
        /// The project's default class. (A project must have at least one class for the fix to be registered).
        /// </summary>
        private const string DefaultClass = @"
namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        /// <summary>
        /// Constructs the expected result when the analyzer is triggered, now with no location.
        /// </summary>
        /// <returns>A <see cref="DiagnosticResult"/> produced by the analyzer.</returns>
        private static DiagnosticResult BuildExpectedResult()
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.NullableReferenceTypesEnabled,
                Message = "Nullable reference types should be enabled",
                Severity = DiagnosticSeverity.Warning,
                // No Locations specified as this is a project-level diagnostic.
            };
        }

        /// <summary>
        /// Asserts no diagnostics are produced if nullable reference types are already enabled.
        /// </summary>
        [TestMethod]
        public void No_Diagnostics_For_Enabled()
        {
            CompilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable);

            VerifyNoDiagnostic(DefaultClass);
        }

        /// <summary>
        /// Asserts no diagnostics are produced if nullable reference types are set to 'annotations'.
        /// </summary>
        [TestMethod]
        public void No_Diagnostics_For_Annotations()
        {
            CompilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Annotations);

            VerifyNoDiagnostic(DefaultClass);
        }

        /// <summary>
        /// Asserts no diagnostics are produced if nullable reference types are set to 'warnings'.
        /// </summary>
        [TestMethod]
        public void No_Diagnostics_For_Warnings()
        {
            CompilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Warnings);

            VerifyNoDiagnostic(DefaultClass);
        }

        /// <summary>
        /// Asserts that a diagnostic is produced if nullable reference types are disabled.
        /// </summary>
        [TestMethod]
        public void Diagnostic_And_Code_Fix_For_Disabled()
        {
            CompilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Disable);

            VerifyDiagnostic(DefaultClass, BuildExpectedResult());

            // The code fix may not actually alter the source code, but just confirm that applying the fix does not break anything.
            VerifyCodeFix(DefaultClass, DefaultClass);
        }

        /// <summary>
        /// Ensures the correct code fix provider is associated with this analyzer.
        /// </summary>
        /// <returns>An instance of the <see cref="NullableReferenceTypesCodeFixProvider"/>.</returns>
        protected override CodeFixProvider GetCSharpCodeFixProvider()
            => new NullableReferenceTypesCodeFixProvider();

        /// <summary>
        /// Ensures the correct analyzer is returned.
        /// </summary>
        /// <returns>An instance of the <see cref="DiagnosticAnalyzer"/>.</returns>
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new NullableReferenceTypesAnalyzer();
    }
}
