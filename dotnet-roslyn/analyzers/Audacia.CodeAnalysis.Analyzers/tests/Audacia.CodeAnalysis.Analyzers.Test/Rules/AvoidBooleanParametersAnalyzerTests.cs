using Audacia.CodeAnalysis.Analyzers.Rules.AvoidBooleanParameter;
using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionProducesResponseType;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class AvoidBooleanParametersAnalyzerTests : DiagnosticVerifier
    {

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new AvoidBooleanParameterAnalyzer();

        private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = AvoidBooleanParameterAnalyzer.Id,
                Severity = AvoidBooleanParameterAnalyzer.Severity,
                Message = message,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
        }

        [TestMethod]
        public void No_Diagnostics_For_Record_With_Positional_Syntax()
        {

            const string testCode = @"  
    namespace ConsoleApplication
    {
        class TypeName
        {

          static void Main(string[] args)
          {

          }

          public record Test(bool shouldBeFine);
        }
    }";

        VerifyNoDiagnostic(testCode);
        }


        [TestMethod]
        public void Diagnostics_For_Method_With_Boolean_Parameters()
        {

            const string testCode = @"  
            namespace ConsoleApplication
                {

                    class TypeName
                    {

                        static void Main(string[] args)
                            {
                                ShouldFail(true);
                            }

                            public static void ShouldFail(bool imNotAllowed)
                            {

                            }
                    }
                }";

            var expectedDiagnostic = BuildExpectedResult("Parameter 'imNotAllowed' is of type 'bool'", 13, 64);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
        
        [TestMethod]
        public void No_Diagnostics_For_Record_With_Multiple_Boolean_Positional_Syntax()
        {

            const string testCode = @"  
    namespace ConsoleApplication
    {
        class TypeName
        {

          static void Main(string[] args)
          {

          }

          public record Test(bool shouldBeFine, bool thisToo);
        }
    }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Private_Record_With_Boolean_Positional_Syntax()
        {

            const string testCode = @"  
    namespace ConsoleApplication
    {
        class TypeName
        {

          static void Main(string[] args)
          {

          }

          private record Test(bool shouldBeFine);
        }
    }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Internal_Record_With_Boolean_Positional_Syntax()
        {

            const string testCode = @"  
    namespace ConsoleApplication
    {
        class TypeName
        {

          static void Main(string[] args)
          {

          }

          internal record Test(bool shouldBeFine);
        }
    }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Primary_Constructor_With_Bool_Parameters()
        {

            const string testCode = @"  
            namespace ConsoleApplication
                {

                    class TypeName
                    {

                        static void Main(string[] args)
                            {
                            }

                        public record PrimaryCon(bool shouldBeOkay)
                            {
                                private bool testVar => shouldBeOkay;
                            }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }
    }
}
