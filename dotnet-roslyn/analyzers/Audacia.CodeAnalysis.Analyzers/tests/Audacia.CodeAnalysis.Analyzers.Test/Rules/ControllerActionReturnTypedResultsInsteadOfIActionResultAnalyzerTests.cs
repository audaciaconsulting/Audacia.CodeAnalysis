using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionReturnTypedResults;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ControllerActionReturnTypedResultsInsteadOfIActionResultAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new ControllerActionReturnTypedResultsInsteadOfIActionResultAnalyzer();

        private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = ControllerActionReturnTypedResultsInsteadOfIActionResultAnalyzer.Id,
                Severity = ControllerActionReturnTypedResultsInsteadOfIActionResultAnalyzer.Severity,
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
            const string testCode = @"
namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Non_Controller_Method()
        {
            const string testCode = @"
using System;

namespace ConsoleApplication1;

public class TestClass
{
    static void Main(string[] args)
    {
        Console.WriteLine(1234567890);
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_TypedResult_ReturnType()
        {
            const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public Results<NotFound, Ok<string>> Get()
    {
        var result = Task.FromResult(""hello"").Result;
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_Async_Method_With_TypedResult_ReturnType()
        {
            const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public async Task<Results<NotFound, Ok<string>>> Get()
    {
        var result = await Task.FromResult(""hello"");
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_With_IActionResult_ReturnType()
        {
            const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(""hello"");
    }
}";

            const string expectedMessage
                = "Controller action name 'Get' should return a TypedResult rather than an IActionResult";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 15, 12);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_WithAsync_Method_with_IActionResult_ReturnType()
        {
            const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await Task.FromResult(""hello"");
        return Ok(result);
    }
}";

            const string expectedMessage
                = "Controller action name 'Get' should return a TypedResult rather than an IActionResult";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 14, 18);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Multiple_Diagnostics_For_Controllers_With_Multiple_Methods_With_IActionResults_ReturnType_Instead_of_TypedResults()
        {
            const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(""hello"");
    }

    [HttpGet]
    public IActionResult Get(string id)
    {
        return Ok(""hello"");
    }
}";
            const string expectedMessage1
                = "Controller action name 'Get' should return a TypedResult rather than an IActionResult";

            const string expectedMessage2
                = "Controller action name 'Get' should return a TypedResult rather than an IActionResult";

            var expectedDiagnostics
                = new[]
                {
                    BuildExpectedResult(expectedMessage1, 14, 12),
                    BuildExpectedResult(expectedMessage2, 20, 12)
                };

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }
    }
}
