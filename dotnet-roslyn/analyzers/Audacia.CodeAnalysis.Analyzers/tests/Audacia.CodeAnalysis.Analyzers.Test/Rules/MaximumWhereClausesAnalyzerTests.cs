using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.ThenByDescendingAfterOrderBy;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;
using Audacia.CodeAnalysis.Analyzers.Rules.MaximumWhereClauses;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class MaximumWhereClausesAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new MaximumWhereClausesAnalyzer();

        private DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = MaximumWhereClausesAnalyzer.Id,
                Severity = ThenByDescendingAfterOrderByAnalyzer.Severity,
                Message = message,
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
            const string testCode = @"";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Where_With_Single_Clause()
        {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public int Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Number=2 },
                                   new TestClass { String = ""cba"", Number = 2 },
                                   new TestClass { String = ""bac"", Number = 2 },
                                   new TestClass { String = ""abc"", Number = 1 } };

                            tests.Where(t => t.String.Length > 1);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Where_With_Two_Clauses()
        {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public int Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Number=2 },
                                   new TestClass { String = ""cba"", Number = 2 },
                                   new TestClass { String = ""bac"", Number = 2 },
                                   new TestClass { String = ""abc"", Number = 1 } };

                            tests.Where(t => true && true);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Where_With_Three_Clauses()
        {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public int Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Number=2 },
                                   new TestClass { String = ""cba"", Number = 2 },
                                   new TestClass { String = ""bac"", Number = 2 },
                                   new TestClass { String = ""abc"", Number = 1 } };

                            tests.Where(t => true && true && true);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostic_For_Where_With_Four_Clauses()
        {
            const string testCode = @"
                using System;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public int Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Number=2 },
                                   new TestClass { String = ""cba"", Number = 2 },
                                   new TestClass { String = ""bac"", Number = 2 },
                                   new TestClass { String = ""abc"", Number = 1 } };

                            tests.Where(t => true && true && true && true);
                        }
                    }
                }";

            const string expectedMessage = "'Where' contains 4 clauses, which exceeds the maximum of 3 clauses per 'Where'.";
            var expectedDiagnostics = BuildExpectedResult(expectedMessage, 22, 29);

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }
    }
}
