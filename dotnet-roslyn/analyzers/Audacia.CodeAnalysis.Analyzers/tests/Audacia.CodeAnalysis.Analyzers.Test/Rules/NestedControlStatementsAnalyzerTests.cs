using Audacia.CodeAnalysis.Analyzers.Rules.NestedControlStatements;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class NestedControlStatementsAnalyzerTests : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NestedControlStatementsAnalyzer();
        }

        private DiagnosticResult BuildExpectedResult(string memberName, int lineNumber, int column, int statementCount, int maxStatementCount = NestedControlStatementsAnalyzer.DefaultMaximumDepth)
        {
            return new DiagnosticResult
            {
                Id = NestedControlStatementsAnalyzer.Id,
                Message = $"{memberName} contains {statementCount} nested control flow statements, which exceeds the maximum of {maxStatementCount} nested control flow statements",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                ]
            };
        }

        [TestMethod]
        public void No_Diagnostics_For_Method_Body_Less_Than_Max_Allowed_Statements()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        while (true) 
        {
            while (true) 
            {
            }
        }
    }
}";

            VerifyNoDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_While_Statements()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        while (true) 
        {
            while (true) 
            {
                while (true) 
                {
                    while (true) 
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("WhileStatement", 12, 17, 3),
                BuildExpectedResult("WhileStatement", 14, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_While_With_Multiple_Statement_In_Block_Statements()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        while (true) 
        {
            while (true) 
            {
                while (true) 
                {
                    while (true) 
                    {
                    }

                    while (true) 
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("WhileStatement", 12, 17, 3),
                BuildExpectedResult("WhileStatement", 14, 21, 4),
                BuildExpectedResult("WhileStatement", 18, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }


        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_Do_Statements()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        do 
        {
            do 
            {
                do 
                {
                    do 
                    {
                    } while (true);
                } while (true);
            } while (true);
        } while (true);
    }
}";
            var expected = new[] {
                BuildExpectedResult("DoStatement", 12, 17, 3),
                BuildExpectedResult("DoStatement", 14, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_If_Statements()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        if (true) 
        {
            if (true) 
            {
                if (true) 
                {
                    if (true) 
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("IfStatement", 12, 17, 3),
                BuildExpectedResult("IfStatement", 14, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_If_Statements_With_Else_Clause()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        if (true) 
        {
            if (true) 
            {
                if (true) 
                {
                }
                else if (false)
                {
                }
                else
                {
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("IfStatement", 12, 17, 3),
                BuildExpectedResult("IfStatement", 15, 22, 3),
                BuildExpectedResult("ElseClause", 18, 17, 3)
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_Statements_Within_Else_Clause()
        {
            var test = @"
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (true)
            {
                if (true)
                {
                }
                else
                {
                    if (true)
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("IfStatement", 15, 21, 3)
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_For_Loops()
        {
            var test = @"
namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++) 
        {
            for (int j = 0; j < 10; j++) 
            {
                for (int k = 0; k < 10; k++) 
                {
                    for (int l = 0; l < 10; l++) 
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("ForStatement", 12, 17, 3),
                BuildExpectedResult("ForStatement", 14, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_ForEach_Loops()
        {
            var test = @"
using System.Linq;

namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        foreach (var i in Enumerable.Range(0, 10)) 
        {
            foreach (var j in Enumerable.Range(0, 10)) 
            {
                foreach (var k in Enumerable.Range(0, 10)) 
                {
                    foreach (var l in Enumerable.Range(0, 10)) 
                    {
                    }
                }
            }
        }
    }
}";
            var expected = new[] {
                BuildExpectedResult("ForEachStatement", 14, 17, 3),
                BuildExpectedResult("ForEachStatement", 16, 21, 4),
            };

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_Switch_Statement()
        {
            var test = @"
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            switch (1)
            {
                case 1:
                    switch (1)
                    {
                        case 1:
                            switch (1)
                            {
                                case 1:
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }
    }
}";

            var expected = BuildExpectedResult("SwitchStatement", 14, 29, 3);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostic_For_Method_Body_With_More_Than_Max_Allowed_Switch_Expressions()
        {
            var test = @"
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = 1 switch
            {
                1 => 1 switch
                {
                    1 => 1 switch
                    {
                        _ => 0,
                    },
                    _ => 0,
                },
                _ => 0,
            };
        }
    }
}";

            var expected = BuildExpectedResult("SwitchExpression", 12, 26, 3);

            VerifyDiagnostic(test, expected);
        }

        [TestMethod]
        public void Diagnostics_For_Method_Body_With_More_Than_Max_Allowed_Switch_Expressions()
        {
            var test = @"
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = 1 switch
            {
                1 => 1 switch
                {
                    1 => 1 switch
                    {
                        1 => 1 switch
                        {
                            _ => 0,
                        },
                        _ => 0,
                    },
                    _ => 0,
                },
                _ => 0,
            };
        }
    }
}";

            var expected = new[]
            {
                BuildExpectedResult("SwitchExpression", 12, 26, 3),
                BuildExpectedResult("SwitchExpression", 14, 30, 4),
            };

            VerifyDiagnostic(test, expected);
        }
    }
}
