using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionProducesResponseType;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

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

class TestClass
{
    static void Main(string[] args)
    {
        TestMethod();
    }

    static void TestMethod()
    {
        Console.WriteLine(1234567890);
    }
}";

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_With_Single_ProducesResponseType_Attribute()
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
        Get();
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public static string Get()
    {
        return ""hello"";
    }
}";

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_With_Multiple_ProducesResponseType_Attributes()
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
        Get();
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static string Get()
    {
        return ""hello"";
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

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
        Get();
    }

    [HttpGet]
    public static string Get()
    {
        return ""hello"";
    }
}";

        const string expectedMessage
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 14, 5);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }

    [TestMethod]
    public void Diagnostics_For_Controller_With_No_ProducesResponseType_And_IActionResult_ReturnType_Attribute()
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
}";

        const string expectedMessage
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 13, 5);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_With_TypedResult_ReturnType_ProducesResponseType_Attributes()
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
    public void Multiple_Diagnostics_For_Multiple_Controllers_With_No_ProducesResponseType_Attribute()
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
    public string Get()
    {
        return ""hello"";
    }

    [HttpGet]
    public string Get(string id)
    {
        return ""hello"";
    }
}";
        const string expectedMessage1
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        const string expectedMessage2
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        var expectedDiagnostics
            = new[]
            {
                BuildExpectedResult(expectedMessage1, 13, 5),
                BuildExpectedResult(expectedMessage2, 19, 5)
            };

        VerifyDiagnostic(testCode, expectedDiagnostics);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_Get_Action_Method_Without_HttpGet_Attribute_And_With_ProducesResponseType_Attribute()
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

    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public string Get()
    {
        return ""hello"";
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

namespace ConsoleApplication1;

public class TestController : ControllerBase
{
    static void Main(string[] args)
    {
    }

    public string Get()
    {
        return ""hello"";
    }
}";

        const string expectedMessage
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 13, 5);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }

    [TestMethod]
    public void No_Diagnostics_For_Controller_Get_Action_Method_Without_ControllerBase_Inheritance_And_With_ProducesResponseType_Attribute()
    {
        const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public string Get()
    {
        return ""hello"";
    }
}";

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostics_For_Controller_Without_ControllerBase_Inheritance_And_Without_ProducesResponseType_Attribute()
    {
        const string testCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConsoleApplication1;

public class TestController
{
    static void Main(string[] args)
    {
    }

    [HttpGet]
    public string Get()
    {
        return ""hello"";
    }
}";

        const string expectedMessage
            = "Controller action name 'Get' has no [ProducesResponseType] attribute";

        var expectedDiagnostic = BuildExpectedResult(expectedMessage, 14, 5);

        VerifyDiagnostic(testCode, expectedDiagnostic);
    }
}