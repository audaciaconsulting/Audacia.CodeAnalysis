using Audacia.CodeAnalysis.Analyzers.Rules.UseRecordTypes;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

[TestClass]
public class UseRecordTypesAnalyzerTests : DiagnosticVerifier
{
    private readonly Mock<ISettingsReader> _mockSettingsReader = new Mock<ISettingsReader>();

    private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string typeName, string suffix)
    {
        return new DiagnosticResult
        {
            Id = UseRecordTypesAnalyzer.Id,
            Message = $"Type '{typeName}' has suffix '{suffix}', which should be record types",
            Severity = DiagnosticSeverity.Warning,
            Locations =
                new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                }
        };
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new UseRecordTypesAnalyzer(_mockSettingsReader.Object);
    }

    [TestMethod]
    public void No_Diagnostic_If_Method_Uses_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private void MethodNameDto()
            {
            }
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Record_Uses_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        record TypeNameDto
        {
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Property_Uses_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string PropertyNameDto { get; set; }
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Interface_Uses_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        interface ITypeNameDto
        {
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void No_Diagnostic_If_Class_Uses_Suffix_To_Lowercase()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNamedto
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

        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNameRequest
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

        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNameDto
        {
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void Diagnostic_If_Class_Uses_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNameDto
        {
        }
    }";

        var expected = BuildExpectedResult(4, 9, "TypeNameDto", "Dto");

        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostic_If_Class_Uses_Suffix_Provided_In_Settings()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("Command");

        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNameCommand
        {
        }
    }";

        var expected = BuildExpectedResult(4, 9, "TypeNameCommand", "Command");

        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostic_If_Class_Name_Matches_Suffix_Exactly()
    {
        _mockSettingsReader.Setup(settings => settings.TryGetValue(
                It.IsAny<SyntaxTree>(),
                new SettingsKey(UseRecordTypesAnalyzer.Id, UseRecordTypesAnalyzer.IncludedSuffixesSetting)))
            .Returns("TypeName");

        var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
        }
    }";

        var expected = BuildExpectedResult(4, 9, "TypeName", "TypeName");

        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostic_If_Class_With_Generic_Argument_Has_Suffix()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeNameDto<T>
        {
        }
    }";

        var expected = BuildExpectedResult(4, 9, "TypeNameDto", "Dto");

        VerifyDiagnostic(test, expected);
    }
}