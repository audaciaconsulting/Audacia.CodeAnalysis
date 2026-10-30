using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audacia.CodeAnalysis.Analyzers.Rules.UseInitForRecordSetters;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
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

    private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
    {
        var diagnosticResult = new DiagnosticResult
        {
            Id = UseInitForRecordSettersAnalyzer.Id,
            Severity = UseInitForRecordSettersAnalyzer.Severity,
            Message = message,
            Locations =
                new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                }
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
        var diagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
        Assert.AreEqual(3, diagnostics.Length);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics[2], (action, _) => actions.Add(action), CancellationToken.None);
        await GetCSharpCodeFixProvider().RegisterCodeFixesAsync(context);
        Assert.AreEqual(1, actions.Count);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id);
        var root = await changedDocument.GetSyntaxRootAsync();
        var formattedRoot = Formatter.Format(root, Formatter.Annotation, changedSolution.Workspace);

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
}
