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
            => new ThenOrderByDescendingAfterOrderByAnalyzer();

        private DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = ThenOrderByDescendingAfterOrderByAnalyzer.Id,
                Severity = ThenOrderByDescendingAfterOrderByAnalyzer.Severity,
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
        public void No_Diagnostics_For_ThenByDescending_Following_OrderBy()
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

                            tests.OrderBy(t => t.Number).ThenByDescending(t => t.String);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_ThenByDescending_Following_OrderByDescending()
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

                            tests.OrderByDescending(t => t.Number).ThenByDescending(t => t.String);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_If_OrderByDescending_Follows_OrderBy()
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

                            tests.OrderBy(t => t.Number).OrderByDescending(t => t.String);
                        }
                    }
                }";

            const string expectedMessage =
                "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 22, 58);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_If_OrderByDescending_Follows_OrderByDescending()
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

                            tests.OrderByDescending(t => t.Number).OrderByDescending(t => t.String);
                        }
                    }
                }";

            const string expectedMessage =
                "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 22, 68);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Multiple_Diagnostics_If_Multiple_OrderByDescending_Statements_Follow_OrderBy()
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

                            tests.OrderBy(t => t.Number).OrderByDescending(t => t.String).OrderByDescending(t => t.Number);
                        }
                    }
                }";

            const string expectedMessage =
                "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";

            var expectedDiagnostics
                = new[]
                {
                    BuildExpectedResult(expectedMessage, 22, 58),
                    BuildExpectedResult(expectedMessage, 22, 91)
                };

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }
    }
}
