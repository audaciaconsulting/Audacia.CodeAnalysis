﻿using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionReturnTypedResults;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

[TestClass]
public class ControllerActionReturnTypedResultsAnalyzerTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new ControllerActionReturnTypedResultsAnalyzer();

    private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
    {
        var diagnosticResult = new DiagnosticResult
        {
            Id = ControllerActionReturnTypedResultsAnalyzer.Id,
            Severity = ControllerActionReturnTypedResultsAnalyzer.Severity,
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
    public void No_Diagnostics_For_Controller_With_TypedResult_ReturnType_Not_Using_ProducesResponseType_Attribute()
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
    public void No_Diagnostics_For_Controller_With_IActionResults_ReturnType_Using_ProducesResponseType_Attribute()
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(""hello"");
    }
}";

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_With_Async_Method_TypedResult_ReturnType_Not_Using_ProducesResponseType_Attribute()
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
    public void Diagnostics_For_Controller_With_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public Results<NotFound, Ok<string>> Get()
    {
        var result = Task.FromResult(""hello"").Result;
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}";

        const string expectedMessage
            = "[ProducesResponseType] attribute should not be applied when using TypedResults";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 16, 6);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }

    [TestMethod]
    public void Diagnostics_For_Controller_With_Async_Method_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<Results<NotFound, Ok<string>>> Get()
    {
        var result = await Task.FromResult(""hello"");
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}";

        const string expectedMessage
            = "[ProducesResponseType] attribute should not be applied when using TypedResults";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 16, 6);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }

    [TestMethod]
    public void Multiple_Diagnostics_For_Controllers_With_Multiple_Methods_With_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<Results<NotFound, Ok<string>>> Get()
    {
        var result = await Task.FromResult(""hello"");
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<Results<NotFound, Ok<string>>> Get(string id)
    {
        var result = await Task.FromResult(""hello"");
        return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}";
        const string expectedMessage1
            = "[ProducesResponseType] attribute should not be applied when using TypedResults";

        const string expectedMessage2
            = "[ProducesResponseType] attribute should not be applied when using TypedResults";

        var expectedDiagnostics
            = new[]
            {
                BuildExpectedResult(expectedMessage1, 16, 6),
                BuildExpectedResult(expectedMessage2, 24, 6)
            };

        VerifyDiagnostic(testCode, expectedDiagnostics);
    }
}