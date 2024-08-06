using Audacia.CodeAnalysis.Analyzers.Rules.AsyncSuffix;
using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionProducesResponseType;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ControllerActionProducesResponseTypeAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new ControllerActionProducesResponseTypeAnalyzer();

        private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = ControllerActionProducesResponseTypeAnalyzer.Id,
                Severity = ControllerActionProducesResponseTypeAnalyzer.Severity,
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
        public void No_Diagnostics_For_Non_Controller_Method()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        void TestMethod()
                        {
                            Console.WriteLine(1234567890);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_Single_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_Multiple_ProducesResponseType_Attributes()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        [ProducesResponseType(StatusCodes.Status404NotFound)]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_With_No_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_With_No_ProducesResponseType_And_IActionResult_ReturnType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public IActionResult Get()
                        {
                            return Ok('hello');
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_TypedResult_ReturnType_ProducesResponseType_Attributes()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public Results<NotFound, Ok<string>> Get()
                        {
                            var result = GetResult();
                            return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Multiple_Diagnostics_For_Multiple_Controllers_With_No_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public string Get()
                        {
                            return 'hello';
                        }

                        [HttpGet]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";
            const string expectedMessage1
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            const string expectedMessage2
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            var expectedDiagnostics
                = new[]
                {
                    BuildExpectedResult(expectedMessage1, 9, 25),
                    BuildExpectedResult(expectedMessage2, 15, 25)
                };

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_Get_Action_Method_Without_HttpGet_Attribute_And_With_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        [ProducesResponseType(StatusCodes.Status404NotFound)]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_Without_HttpGet_Attribute_And_Without_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_Get_Action_Method_Without_ControllerBase_Inheritance_And_With_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        [ProducesResponseType(StatusCodes.Status404NotFound)]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_Without_ControllerBase_Inheritance_And_Without_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController
                    {
                        [HttpGet]
                        public string Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' has no [ProducesResponseType] attribute";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }
    }
}