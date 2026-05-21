using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Audacia.CodeAnalysis.Analyzers.Rules.TestNamesMustBeConsistent
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TestNamesMustBeConsistentAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.TestNamesMustBeConsistent;

        private const string Title = "Test method names must be consistent";
        private const string MessageFormat = "Test method name '{0}' is not consistent with configured format '{1}'";
        private const string Description = "When a test method name does not follow the configured naming conventions, it should be updated to be consistent.";

        public const string FormatConfigKey = "test_method_name_format";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(Id, Title, MessageFormat, DiagnosticCategory.Naming, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public TestNamesMustBeConsistentAnalyzer()
        {
        }

        public TestNamesMustBeConsistentAnalyzer(ISettingsReader settingsReader)
        {
            _settingsReader = settingsReader;
            _isSettingsReaderInjected = true;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(RegisterCompilationStart);
        }

        private void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            // We don't want to reinitialize if the ISettingsReader object was injected as that means we're in a unit test
            if (_settingsReader == null || !_isSettingsReaderInjected)
            {
                _settingsReader = new EditorConfigSettingsReader(startContext.Options);
            }

            startContext.RegisterSyntaxNodeAction(actionContext => AnalyzeMethodDeclaration(actionContext), SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {   
            var isTestMethod = nodeAnalysisContext.IsXunitTestMethod();
            if (!isTestMethod)
            {
                return;
            }

            var methodNameRegex = _settingsReader.TryGetValue(nodeAnalysisContext.Node.SyntaxTree, new SettingsKey(Id, FormatConfigKey));

            if(string.IsNullOrEmpty(methodNameRegex))
            {
                return;
            }

            var methodDeclaration = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var validMethodName = Regex.IsMatch(methodDeclaration.Identifier.Text, methodNameRegex);

            if (!validMethodName)
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text, methodNameRegex);
                nodeAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
