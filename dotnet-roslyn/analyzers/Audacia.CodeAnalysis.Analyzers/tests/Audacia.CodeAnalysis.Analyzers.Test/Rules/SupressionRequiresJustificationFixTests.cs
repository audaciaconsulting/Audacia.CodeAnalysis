using Audacia.CodeAnalysis.Analyzers.Rules.MustHaveJustification;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

/// <summary>
/// Unit tests for the <see cref="SupressionRequiresJustificationAnalyzer"/>.
/// </summary>
[TestClass]
public class SupressionRequiresJustificationFixProviderTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SupressionRequiresJustificationAnalyzer();
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new SupressionRequiresJustificationFixProvider();
    }

    [TestMethod]
    public void Diagnostics_For_Suppress_Message_Attribute_With_No_Justification()
    {
        const string testFileContents = @"
using System.Diagnostics.CodeAnalysis;

namespace ConsoleApplication1;

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

        const string fixedTestFileContents = @"
using System.Diagnostics.CodeAnalysis;

namespace ConsoleApplication1;

public class TestClass 
{
    [SuppressMessageAttribute(""Unit Tests"", ""IDE0051"", Justification = ""<Pending>"")]
    private readonly string _unusedMember;

    public TestClass() 
    {

    }

    static void Main(string[] args)
    {
    
    }
}";
        VerifyCodeFix(testFileContents, fixedTestFileContents);
    }

    [DataTestMethod]
    [DataRow("MaxMethodLengthAttribute")]
    [DataRow("MaxParameterCountAttribute")]
    public void Diagnostics_For_Audacia_Code_Analysis_Attribute_With_No_Justification(string attributeName)
    {
        var testFileContents = $@"
using Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount;

namespace ConsoleApplication1;

public class TestClass 
{{
    private readonly string _unusedMember;

    public TestClass() 
    {{

    }}

    [{attributeName}(20)]
    static void Main(string[] args)
    {{
    
    }}
}}";

        var fixedTestFileContents = $@"
using Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount;

namespace ConsoleApplication1;

public class TestClass 
{{
    private readonly string _unusedMember;

    public TestClass() 
    {{

    }}

    [{attributeName}(20, Justification = ""<Pending>"")]
    static void Main(string[] args)
    {{
    
    }}
}}";
        VerifyCodeFix(testFileContents, fixedTestFileContents);
    }
}