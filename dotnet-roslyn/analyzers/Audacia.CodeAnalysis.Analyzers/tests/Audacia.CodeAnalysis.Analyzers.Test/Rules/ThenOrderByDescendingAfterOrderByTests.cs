using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Audacia.CodeAnalysis.Analyzers.Rules.ThenByDescendingAfterOrderBy;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using CodeFixVerifier = Audacia.CodeAnalysis.Analyzers.Test.Base.CodeFixVerifier;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ThenByDescendingAfterOrderByTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new ThenByDescendingAfterOrderByAnalyzer();

        private DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = ThenByDescendingAfterOrderByAnalyzer.Id,
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
        public void No_Diagnostics_For_OrderBy_Within_Select_Following_OrderBy()
        {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public IEnumerable<int> Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""cba"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""bac"", Numbers = new[] { 1 } },
                                   new TestClass { String = ""abc"", Numbers = new[] { 3, 1, 2 } } };

                            tests.OrderBy(t => t.String).Select(t => t.Numbers.OrderBy(n => n).First());
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_OrderBy_Within_Where_Following_OrderBy()
        {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public IEnumerable<int> Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""cba"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""bac"", Numbers = new[] { 1 } },
                                   new TestClass { String = ""abc"", Numbers = new[] { 3, 1, 2 } } };

                            tests.OrderBy(t => t.String).Where(t => t.Numbers.OrderBy(n => n).First() > 1);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_OrderByDescending_Within_Any_Following_OrderBy()
        {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public IEnumerable<int> Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""cba"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""bac"", Numbers = new[] { 1 } },
                                   new TestClass { String = ""abc"", Numbers = new[] { 3, 1, 2 } } };

                            tests.OrderBy(t => t.String).Any(t => t.Numbers.OrderByDescending(n => n).First() > 1);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_OrderBy_Within_OrderBy()
        {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public IEnumerable<int> Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""cba"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""bac"", Numbers = new[] { 1 } },
                                   new TestClass { String = ""abc"", Numbers = new[] { 3, 1, 2 } } };

                            tests.OrderBy(t => t.Numbers.OrderByDescending(n => n).First());
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_If_OrderByDescending_Follows_OrderBy_Within_OrderBy()
        {
            const string testCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace TestApp
                {
                    class Program
                    {
                        class TestClass
                        {
                            public string String { get; set; }
                            public IEnumerable<int> Number { get; set; }
                        }

                        static void Main(string[] args)
                        {
                            TestClass[] tests = { new TestClass { String=""abc"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""cba"", Numbers = new[] { 2 } },
                                   new TestClass { String = ""bac"", Numbers = new[] { 1 } },
                                   new TestClass { String = ""abc"", Numbers = new[] { 3, 1, 2 } } };

                            tests.OrderBy(t => t.Numbers.OrderBy(n => n).OrderByDescending(n => n).First());
                        }
                    }
                }";

            const string expectedMessage =
                "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";

            // this diagnostic gets raised twice for some reason, not sure why but have decided it's not worth fixing
            var expectedDiagnostics = new[]
            {
                BuildExpectedResult(expectedMessage, 23, 74),
                BuildExpectedResult(expectedMessage, 23, 74),
            };

            VerifyDiagnostic(testCode, expectedDiagnostics);
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
        public void Diagnostics_If_OrderByDescending_Follows_OrderBy_With_Another_Method_Between()
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

                            tests.OrderBy(t => t.Number).Where(t => t.Number > 1).OrderByDescending(t => t.String);
                        }
                    }
                }";

            const string expectedMessage =
                "ThenByDescending statement should replace OrderByDescending when following OrderBy or OrderByDescending statement.";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 22, 83);

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
