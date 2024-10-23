using Audacia.CodeAnalysis.Analyzers.Rules.UseRecordTypes;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

[TestClass]
public class UseRecordTypesAnalyzerTests : CodeFixVerifier
{
    public UseRecordTypesAnalyzerTests()
    {
        ParseOptions = new CSharpParseOptions(LanguageVersion.Latest);
    }

    private readonly Mock<ISettingsReader> _mockSettingsReader = new();

    private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string typeName, string suffix)
    {
        return new DiagnosticResult
        {
            Id = UseRecordTypesAnalyzer.Id,
            Message = $"Type '{typeName}' has suffix '{suffix}', which should be a record type",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", lineNumber, column)
            ]
        };
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new UseRecordTypesAnalyzer(_mockSettingsReader.Object);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new UseRecordTypesCodeFixProvider();
    }

    [TestMethod]
    public void No_Diagnostic_If_Method_Uses_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

class TypeName
{
    private void MethodNameDto()
    {
    }

    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Record_Uses_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

record TypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Property_Uses_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

class TypeName
{
    public string PropertyNameDto { get; set; }

    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Interface_Uses_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

interface ITypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Class_Uses_Suffix_To_Lowercase()
    {
        const string test = @"
namespace ConsoleApplication1;

class TypeNamedto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Class_Uses_Suffix_Not_Specified_In_Settings()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("Command");

        const string test = @"
namespace ConsoleApplication1;

class TypeNameRequest
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_For_Dto_If_Settings_Are_Overridden()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("Request");

        const string test = @"
namespace ConsoleApplication1;

class TypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Editor_Config_Specified_Invalid_Character()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns(".");

        const string test = @"
namespace ConsoleApplication1;

class TypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Using_Unsupported_Language_Version()
    {
        // Set the C# language version to a version before records were introduced
        ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp8);

        const string test = @"
namespace ConsoleApplication1
{
    class TypeNameDto
    {
    }
    
    public class TestClass
    {
        static void Main(string[] args)
        {
        }
    }
}";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Class_Uses_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

class TypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        var expected = BuildExpectedResult(
            lineNumber: 4, column: 5, "TypeNameDto", "Dto");

        VerifyDiagnostic(test, expected);

        const string fixedTestCode = @"
namespace ConsoleApplication1;

record TypeNameDto
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";
        VerifyCodeFix(test, fixedTestCode);
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Class_Uses_Suffix_Provided_In_Settings()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("Command,Request");

        const string test = @"
namespace ConsoleApplication1;

class TypeNameCommand
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        var expected = BuildExpectedResult(
            lineNumber: 4, column: 5, "TypeNameCommand", "Command");

        VerifyDiagnostic(test, expected);

        const string fixedTestCode = @"
namespace ConsoleApplication1;

record TypeNameCommand
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";
        VerifyCodeFix(test, fixedTestCode);
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Class_Name_Matches_Suffix_Exactly()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("TypeName");

        const string test = @"
namespace ConsoleApplication1;

class TypeName
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        var expected = BuildExpectedResult(
            lineNumber: 4, column: 5, "TypeName", "TypeName");

        VerifyDiagnostic(test, expected);

        const string fixedTestCode = @"
namespace ConsoleApplication1;

record TypeName
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";
        VerifyCodeFix(test, fixedTestCode);
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_For_Class_With_Primary_Constructor()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("TypeName");

        const string test = @"
namespace ConsoleApplication1;

class TypeName(int Property1, int Property2)
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        var expected = BuildExpectedResult(
            lineNumber: 4, column: 5, "TypeName", "TypeName");

        VerifyDiagnostic(test, expected);

        const string fixedTestCode = @"
namespace ConsoleApplication1;

record TypeName(int Property1, int Property2)
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";
        VerifyCodeFix(test, fixedTestCode);
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Class_With_Generic_Argument_Has_Suffix()
    {
        const string test = @"
namespace ConsoleApplication1;

class TypeNameDto<T>
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";

        var expected = BuildExpectedResult(
            lineNumber: 4, column: 5, "TypeNameDto", "Dto");

        VerifyDiagnostic(test, expected);

        const string fixedTestCode = @"
namespace ConsoleApplication1;

record TypeNameDto<T>
{
}

public class TestClass
{
    static void Main(string[] args)
    {
    }
}";
        VerifyCodeFix(test, fixedTestCode);
    }
}