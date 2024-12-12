using System.Diagnostics.CodeAnalysis;
using Audacia.CodeAnalysis.Analyzers.Rules.MustHaveJustification;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.Mime.MediaTypeNames;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

/// <summary>
/// Unit tests for the <see cref="SupressionRequiresJustificationAnalyzer"/>.
/// </summary>
[TestClass]
public class SupressionRequiresJustificationAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SupressionRequiresJustificationAnalyzer();
    }

    [DataTestMethod]
    [DataRow("MaxMethodLengthAttribute")]
    [DataRow("MaxParameterCountAttribute")]
    public void Diagnostics_For_Audacia_Code_Analysis_Attribute_With_No_Justification(string attributeName)
    {
        var testFileContents = $@"
using Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount;
using System.Diagnostics.CodeAnalysis;

namespace TestNamespace;

public class TestClass 
{{
    public TestClass() 
    {{

    }}

    [{attributeName}(20)]
    static void Main(string[] args)
    {{ 
    
    }}
}}";
        const int errorLine = 15;
        const int errorColumn = 6;

        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = $"{attributeName} is missing a value for 'Justification'",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [TestMethod]
    public void Diagnostics_For_Suppress_Message_Attribute_With_No_Justification()
    {
        var testFileContents = @"
using System.Diagnostics.CodeAnalysis;

namespace TestNamespace;

public class TestClass 
{
    [SuppressMessageAttribute(""Unit Tests"", ""IDE0051"")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";
        const int errorLine = 8;
        const int errorColumn = 6;
        const string errorMessage = "SuppressMessageAttribute is missing a value for 'Justification'";
        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = errorMessage,
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [DataRow("MaxLengthSkip", "MaxMethodLengthAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength")]
    [DataRow("MaxParamSkip", "MaxParameterCountAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount")]
    [TestMethod]
    public void Diagnostics_For_Audacia_Code_Analysis_Attribute_Alias_With_No_Justification(string alias, string attributeName, string attributeNameSpace)
    {
        var testFileContents = $@"
using {alias} = {attributeNameSpace}.{attributeName};


namespace TestNamespace;

public class TestClass 
{{
    public TestClass() 
    {{

    }}

    [{alias}(20)]
    static void Main(string[] args)
    {{ 
    
    }}
}}";
        const int errorLine = 14;
        const int errorColumn = 6;

        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = $"{attributeName} is missing a value for 'Justification'",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [TestMethod]
    public void Diagnostics_For_SuppressMessage_Attribute_Alias_With_No_Justification()
    {
        var testFileContents = @"
using Supress = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace TestNamespace;

public class TestClass 
{
    [Supress(""Unit Tests"", ""IDE0051"")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";
        const int errorLine = 8;
        const int errorColumn = 6;
        const string errorMessage = "SuppressMessageAttribute is missing a value for 'Justification'";
        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = errorMessage,
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [TestMethod]
    public void Diagnostics_For_Suppress_Message_Attribute_With_Placeholder_Justification()
    {
        var testFileContents = @"
using System.Diagnostics.CodeAnalysis;

namespace TestNamespace;

public class TestClass 
{
    [SuppressMessageAttribute(""Unused Private Variable"", ""IDE0051"", Justification = ""<Pending>"")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";
        const int errorLine = 8;
        const int errorColumn = 6;
        const string errorMessage = "SuppressMessageAttribute is missing a value for 'Justification'";
        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = errorMessage,
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [DataRow("MaxLengthSkip", "MaxMethodLengthAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength")]
    [DataRow("MaxParamSkip", "MaxParameterCountAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount")]
    [TestMethod]
    public void Diagnostics_For_Audacia_Code_Analysis_Attribute_Alias_With_Placeholder_Justification(string alias, string attributeName, string attributeNameSpace)
    {
        var testFileContents = $@"
using {alias} = {attributeNameSpace}.{attributeName};


namespace TestNamespace;

public class TestClass 
{{
    public TestClass() 
    {{

    }}

    [{alias}(20, Justification = ""<Pending>"")]
    static void Main(string[] args)
    {{ 
    
    }}
}}";
        const int errorLine = 14;
        const int errorColumn = 6;

        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = $"{attributeName} is missing a value for 'Justification'",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [TestMethod]
    public void Diagnostics_For_Suppress_Message_Attribute_Alias_With_Placeholder_Justification()
    {
        var testFileContents = @"
using Supress = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace TestNamespace;

public class TestClass 
{
    [Supress(""Unused Private Variable"", ""IDE0051"", Justification = ""<Pending>"")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";
        const int errorLine = 8;
        const int errorColumn = 6;
        const string errorMessage = "SuppressMessageAttribute is missing a value for 'Justification'";
        var expected = new DiagnosticResult
        {
            Id = SupressionRequiresJustificationAnalyzer.Id,
            Message = errorMessage,
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", errorLine, errorColumn)
            ]
        };

        VerifyDiagnostic(testFileContents, expected);
    }

    [DataTestMethod]
    [DataRow("MaxMethodLengthAttribute")]
    [DataRow("MaxParameterCountAttribute")]
    public void No_Diagnostics_For_Audacia_Code_Analysis_Attribute_With_Justification(string attributeName)
    {
        var testFileContents = $@"
using Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount;
using System.Diagnostics.CodeAnalysis;

namespace TestNamespace;

public class TestClass 
{{
    public TestClass() 
    {{

    }}

    [{attributeName}(20, Justification = ""Suppressed for the purpose of a passing unit test."")]
    static void Main(string[] args)
    {{ 
    
    }}
}}";

        VerifyNoDiagnostic(testFileContents);
    }

    [TestMethod]
    public void No_Diagnostics_For_Suppress_Message_Attribute_With_Justification()
    {
        var testFileContents = @"
using System.Diagnostics.CodeAnalysis;

namespace TestNamespace;

public class TestClass 
{

    [SuppressMessageAttribute(""Unused Private Variable"", ""IDE0051"", Justification = ""Suppressed for the purpose of a passing unit test."")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";

        VerifyNoDiagnostic(testFileContents);
    }

    [DataRow("MaxLengthSkip", "MaxMethodLengthAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength")]
    [DataRow("MaxParamSkip", "MaxParameterCountAttribute", "Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount")]
    [TestMethod]
    public void No_Diagnostics_For_Audacia_Code_Analysis_Attribute_Alias_With_No_Justification(string alias, string attributeName, string attributeNameSpace)
    {
        var testFileContents = $@"
using {alias} = {attributeNameSpace}.{attributeName};


namespace TestNamespace;

public class TestClass 
{{
    public TestClass() 
    {{

    }}

    [{alias}(20, Justification = ""Suppressed for the purpose of a passing unit test."")]
    static void Main(string[] args)
    {{ 
    
    }}
}}";

        VerifyNoDiagnostic(testFileContents);
    }

    [TestMethod]
    public void No_Diagnostics_For_Suppress_Message_Attribute_Alias_With_No_Justification()
    {
        var testFileContents = @"
using Supress = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace TestNamespace;

public class TestClass 
{
    [Supress(""Unused Private Variable"", ""IDE0051"", Justification = ""Suppressed for the purpose of a passing unit test."")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";

        VerifyNoDiagnostic(testFileContents);
    }
}
