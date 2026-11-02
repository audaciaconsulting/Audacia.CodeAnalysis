using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audacia.CodeAnalysis.Analyzers.Core.Rules.UseInitForRecordSetters;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

[TestClass]
public class UseInitForRecordSettersAnalyzerTests : CodeFixVerifier
{
    private const string EntryPoint = @"
class Program
{
    static void Main(string[] args)
    {
    }
}";

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new UseInitForRecordSettersAnalyzer();

    protected override CodeFixProvider GetCSharpCodeFixProvider()
        => new UseInitForRecordSettersFixProvider();

    private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column, string filePath = "Test0.cs")
    {
        var diagnosticResult = new DiagnosticResult
        {
            Id = UseInitForRecordSettersAnalyzer.Id,
            Severity = UseInitForRecordSettersAnalyzer.Severity,
            Message = message,
            Locations =
                [
                    new DiagnosticResultLocation(filePath, lineNumber, column)
                ]
        };

        return diagnosticResult;
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Set_Used_For_Record_Setter()
    {
        var testCode = @"
record TypeName1
{
    public string Name { get; set; }
}" + EntryPoint;

        var expectedDiagnostic = BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 4, 31);

        VerifyDiagnostic(testCode, expectedDiagnostic);

        var fixedCode = @"
record TypeName1
{
    public string Name { get; init; }
}" + EntryPoint;

        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void No_Diagnostic_If_Set_Used_For_Class_Setter()
    {
        var testCode = @"
class TypeName1
{
    public string Name { get; set; }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_For_Each_Record_Property_With_Setter()
    {
        var testCode = @"
record TypeName1
{
    public string First { get; set; }
    public string Second { get; set; }
    public string Third { get; set; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'First' in record type 'TypeName1' should be init-only", 4, 32),
            BuildExpectedResult("Record property 'Second' in record type 'TypeName1' should be init-only", 5, 33),
            BuildExpectedResult("Record property 'Third' in record type 'TypeName1' should be init-only", 6, 32));
    }

    [TestMethod]
    public void No_Diagnostic_If_Record_Property_Is_Already_Init_Only()
    {
        var testCode = @"
record TypeName1
{
    public string Name { get; init; }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostic_If_Record_Uses_Positional_Syntax()
    {
        var testCode = @"
record TypeName1(string First, string Second);" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_Only_For_Setters_Interspersed_With_Init_Accessors()
    {
        var testCode = @"
record TypeName1
{
    public string First { get; init; }
    public string Second { get; set; }
    public string Third { get; init; }
    public string Fourth { get; set; }
    public string Fifth { get; init; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Second' in record type 'TypeName1' should be init-only", 5, 33),
            BuildExpectedResult("Record property 'Fourth' in record type 'TypeName1' should be init-only", 7, 33));
    }

    [TestMethod]
    public void No_Diagnostic_For_Set_Text_Elsewhere_In_Record()
    {
        var testCode = @"
record Asset
{
    private readonly string asset = ""set"";
    public string AssetName { get; init; }
    public string @set => asset;

    // Reset the asset without a setter.
    public string Reset(string offset) => asset + offset + ""set"";
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public async Task Code_Fix_Only_Replaces_Third_Selected_Setter()
    {
        var testCode = @"
record TypeName1
{
    public string First { get; set; }
    public string Second { get; set; }
    public string Third { get; set; }
}" + EntryPoint;

        var fixedCode = @"
record TypeName1
{
    public string First { get; set; }
    public string Second { get; set; }
    public string Third { get; init; }
}" + EntryPoint;

        var analyzer = GetCSharpDiagnosticAnalyzer();
        var document = CreateDocument(testCode);
        var diagnostics = GetSortedDiagnosticsFromDocuments(analyzer, [document]);
        Assert.HasCount(3, diagnostics);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics[2], (action, _) => actions.Add(action), CancellationToken.None);
        await GetCSharpCodeFixProvider().RegisterCodeFixesAsync(context);
        Assert.HasCount(1, actions);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id);
        var root = await changedDocument.GetSyntaxRootAsync(CancellationToken.None);
        var formattedRoot = Formatter.Format(root, Formatter.Annotation, changedSolution.Workspace, null, CancellationToken.None);

        Assert.AreEqual(fixedCode, formattedRoot.ToFullString());

        VerifyDiagnostic(formattedRoot.ToFullString(),
            BuildExpectedResult("Record property 'First' in record type 'TypeName1' should be init-only", 4, 32),
            BuildExpectedResult("Record property 'Second' in record type 'TypeName1' should be init-only", 5, 33));
    }

    [TestMethod]
    public void Code_Fix_Preserves_Other_Set_Text_In_Record()
    {
        var testCode = @"
record Asset
{
    private string asset = ""set"";
    public string AssetName
    {
        get => asset;
        // Reset the asset in the setter.
        set => asset = value + ""set"";
    }
    public string Offset { get; init; }
    public string @set => asset;
    public string Reset(string offset) => asset + offset + ""set"";
}" + EntryPoint;

        var fixedCode = @"
record Asset
{
    private string asset = ""set"";
    public string AssetName
    {
        get => asset;
        // Reset the asset in the setter.
        init => asset = value + ""set"";
    }
    public string Offset { get; init; }
    public string @set => asset;
    public string Reset(string offset) => asset + offset + ""set"";
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'AssetName' in record type 'Asset' should be init-only", 9, 9));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    [DataRow("private", 39)]
    [DataRow("protected", 41)]
    public void Diagnostic_And_Code_Fix_Preserve_Setter_Accessibility(string accessibility, int column)
    {
        var testCode = @"
record TypeName1
{
    public string Name { get; ACCESSIBILITY set; }
}".Replace("ACCESSIBILITY", accessibility) + EntryPoint;

        var fixedCode = @"
record TypeName1
{
    public string Name { get; ACCESSIBILITY init; }
}".Replace("ACCESSIBILITY", accessibility) + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 4, column));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void Code_Fix_Preserves_Block_Body_And_Comments()
    {
        var testCode = @"
record TypeName1
{
    private string name;
    public string Name
    {
        get => name;
        // Keep this setter comment.
        set
        {
            // Keep this body comment.
            name = value.Trim();
        }
    }
}" + EntryPoint;

        var fixedCode = @"
record TypeName1
{
    private string name;
    public string Name
    {
        get => name;
        // Keep this setter comment.
        init
        {
            // Keep this body comment.
            name = value.Trim();
        }
    }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 9, 9));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void Code_Fix_Preserves_Setter_Attributes()
    {
        var testCode = @"
record TypeName1
{
    public string Name
    {
        get;
        [System.Diagnostics.DebuggerStepThrough]
        set;
    }
}" + EntryPoint;

        var fixedCode = @"
record TypeName1
{
    public string Name
    {
        get;
        [System.Diagnostics.DebuggerStepThrough]
        init;
    }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 8, 9));

        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void Code_Fix_Only_Changes_Selected_Record_In_File()
    {
        var testCode = @"
record FirstRecord
{
    public string Name { get; set; }
}
record SecondRecord
{
    public string Name { get; set; }
}" + EntryPoint;

        var fixedCode = @"
record FirstRecord
{
    public string Name { get; init; }
}
record SecondRecord
{
    public string Name { get; set; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'FirstRecord' should be init-only", 4, 31),
            BuildExpectedResult("Record property 'Name' in record type 'SecondRecord' should be init-only", 8, 31));
        VerifyCodeFix(testCode, fixedCode, codeFixIndex: 0);
        VerifyDiagnostic(fixedCode,
            BuildExpectedResult("Record property 'Name' in record type 'SecondRecord' should be init-only", 8, 31));
    }

    [TestMethod]
    public void No_Diagnostic_For_Class_Nested_In_Record()
    {
        var testCode = @"
record TypeName1
{
    public class NestedClass
    {
        public string Name { get; set; }
    }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_Once_For_Each_Property_Across_Partial_Record_Files()
    {
        var firstPart = @"
partial record TypeName1
{
    public string First { get; set; }
}" + EntryPoint;

        var secondPart = @"
partial record TypeName1
{
    public string Second { get; set; }
}";

        VerifyDiagnostic([firstPart, secondPart],
            BuildExpectedResult("Record property 'First' in record type 'TypeName1' should be init-only", 4, 32),
            BuildExpectedResult("Record property 'Second' in record type 'TypeName1' should be init-only", 4, 33, "Test1.cs"));
    }

    [TestMethod]
    public void Diagnostic_And_Code_Fix_If_Set_Used_For_Record_Struct_Setter()
    {
        var testCode = @"
record struct TypeName1
{
    public string Name { get; set; }
}" + EntryPoint;

        var fixedCode = @"
record struct TypeName1
{
    public string Name { get; init; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 4, 31));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void No_Diagnostic_For_Positional_Record_Struct_Property()
    {
        var testCode = @"
record struct TypeName1(string Name);" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_For_Body_Property_In_Positional_Record()
    {
        var testCode = @"
record TypeName1(string First)
{
    public string Second { get; set; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Second' in record type 'TypeName1' should be init-only", 4, 33));
    }

    [TestMethod]
    public void No_Diagnostic_For_Implicit_Interface_Property_Implementation()
    {
        var testCode = @"
interface INamed
{
    string Name { get; set; }
}
record TypeName1 : INamed
{
    public string Name { get; set; }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostic_For_Explicit_Interface_Property_Implementation()
    {
        var testCode = @"
interface INamed
{
    string Name { get; set; }
}
record TypeName1 : INamed
{
    string INamed.Name { get; set; }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_For_Property_Not_Implementing_Interface_Setter()
    {
        var testCode = @"
interface INamed
{
    string Name { get; }
}
record TypeName1 : INamed
{
    public string Name { get; set; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'TypeName1' should be init-only", 8, 31));
    }

    [TestMethod]
    public void No_Diagnostic_For_Base_Property_Implementing_Interface_Setter_In_Derived_Record()
    {
        var testCode = @"
interface INamed
{
    string Name { get; set; }
}
record BaseRecord
{
    public string Name { get; set; }
}
record DerivedRecord : BaseRecord, INamed
{
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostic_For_Base_Property_When_Override_Implements_Interface_Setter()
    {
        var testCode = @"
interface INamed
{
    string Name { get; set; }
}
abstract record BaseRecord
{
    public abstract string Name { get; set; }
}
record DerivedRecord : BaseRecord, INamed
{
    public override string Name { get; set; }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_For_Base_Property_Implementing_Getter_Only_Interface_In_Derived_Record()
    {
        var testCode = @"
interface INamed
{
    string Name { get; }
}
record BaseRecord
{
    public string Name { get; set; }
}
record DerivedRecord : BaseRecord, INamed
{
}" + EntryPoint;

        var fixedCode = @"
interface INamed
{
    string Name { get; }
}
record BaseRecord
{
    public string Name { get; init; }
}
record DerivedRecord : BaseRecord, INamed
{
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'BaseRecord' should be init-only", 8, 31));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public async Task No_Code_Fix_When_Derived_Record_In_Other_Project_Implements_Interface_Setter()
    {
        var baseProject = CreateProject([@"
public interface INamed
{
    string Name { get; set; }
}
public record BaseRecord
{
    public string Name { get; set; }
}" + EntryPoint]);

        var derivedRecord = @"
public record DerivedRecord : BaseRecord, INamed
{
}";

        var derivedProjectId = ProjectId.CreateNewId();
        var solution = baseProject.Solution
            .AddProject(derivedProjectId, "DerivedProject", "DerivedProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(derivedProjectId, baseProject.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(derivedProjectId, baseProject.ParseOptions)
            .AddMetadataReferences(derivedProjectId, baseProject.MetadataReferences)
            .AddProjectReference(derivedProjectId, new ProjectReference(baseProject.Id))
            .AddDocument(DocumentId.CreateNewId(derivedProjectId), "Derived.cs", derivedRecord);

        var baseDocument = solution.GetProject(baseProject.Id).Documents.Single();
        var diagnostics = GetSortedDiagnosticsFromDocuments(GetCSharpDiagnosticAnalyzer(), [baseDocument]);
        Assert.HasCount(1, diagnostics);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(baseDocument, diagnostics[0], (action, _) => actions.Add(action), CancellationToken.None);
        await GetCSharpCodeFixProvider().RegisterCodeFixesAsync(context);

        Assert.IsEmpty(actions);
    }

    [TestMethod]
    public void No_Diagnostic_For_Record_Indexer()
    {
        var testCode = @"
record TypeName1
{
    public int this[int index] { get => index; set { } }
}" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void No_Diagnostic_For_Readonly_Positional_Record_Struct()
    {
        var testCode = @"
readonly record struct TypeName1(string Name);" + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    [DataRow("record")]
    [DataRow("record struct")]
    public void No_Diagnostic_For_Static_Record_Property(string recordDeclaration)
    {
        var testCode = @"
RECORD_DECLARATION TypeName1
{
    public static string Name { get; set; }
}".Replace("RECORD_DECLARATION", recordDeclaration) + EntryPoint;

        VerifyNoDiagnostic(testCode);
    }

    [TestMethod]
    public void Diagnostic_Only_For_Base_Property_When_Overridden_In_Derived_Record()
    {
        var testCode = @"
abstract record BaseRecord
{
    public abstract string Name { get; set; }
}
record DerivedRecord : BaseRecord
{
    public override string Name { get; set; }
}" + EntryPoint;

        var fixedCode = @"
abstract record BaseRecord
{
    public abstract string Name { get; init; }
}
record DerivedRecord : BaseRecord
{
    public override string Name { get; init; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'BaseRecord' should be init-only", 4, 40));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public void Code_Fix_Updates_Every_Override_In_Chain()
    {
        var testCode = @"
record BaseRecord
{
    public virtual string Name { get; set; }
}
record MiddleRecord : BaseRecord
{
    private string name;
    public override string Name
    {
        get => name;
        set
        {
            name = value.Trim();
        }
    }
}
record GetterOnlyRecord : MiddleRecord
{
    public override string Name => ""Fixed"";
}
record DerivedRecord : GetterOnlyRecord
{
    public override string Name { get; set; }
}" + EntryPoint;

        var fixedCode = @"
record BaseRecord
{
    public virtual string Name { get; init; }
}
record MiddleRecord : BaseRecord
{
    private string name;
    public override string Name
    {
        get => name;
        init
        {
            name = value.Trim();
        }
    }
}
record GetterOnlyRecord : MiddleRecord
{
    public override string Name => ""Fixed"";
}
record DerivedRecord : GetterOnlyRecord
{
    public override string Name { get; init; }
}" + EntryPoint;

        VerifyDiagnostic(testCode,
            BuildExpectedResult("Record property 'Name' in record type 'BaseRecord' should be init-only", 4, 39));
        VerifyCodeFix(testCode, fixedCode);
    }

    [TestMethod]
    public async Task Code_Fix_Updates_Overrides_In_Other_Files_And_Compiles()
    {
        var baseFile = @"
abstract record BaseRecord
{
    public abstract string Name { get; set; }
}" + EntryPoint;

        var derivedFile = @"
record DerivedRecord : BaseRecord
{
    public override string Name { get; set; }
}";

        var expectedDerivedFile = @"
record DerivedRecord : BaseRecord
{
    public override string Name { get; init; }
}";

        var project = CreateProject([baseFile, derivedFile]);
        var baseDocument = project.Documents.First();
        var diagnostics = GetSortedDiagnosticsFromDocuments(GetCSharpDiagnosticAnalyzer(), project.Documents.ToArray());
        Assert.HasCount(1, diagnostics);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(baseDocument, diagnostics[0], (action, _) => actions.Add(action), CancellationToken.None);
        await GetCSharpCodeFixProvider().RegisterCodeFixesAsync(context);
        Assert.HasCount(1, actions);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var changedProject = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetProject(project.Id);
        var changedDerivedDocument = changedProject.Documents.Single(d => d.Id != baseDocument.Id);
        var compilation = await changedProject.GetCompilationAsync(CancellationToken.None);

        Assert.AreEqual(expectedDerivedFile, (await changedDerivedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.IsEmpty(compilation.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }
}
