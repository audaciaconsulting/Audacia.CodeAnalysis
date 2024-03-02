using System;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Audacia.CodeAnalysis.Analyzers.Rules.UseRecordTypes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseRecordTypesAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.UseRecordTypes;
        public const string IncludedSuffixesSetting = "included_suffixes";

        private const string Title = "Type should be a record";
        private const string MessageFormat = "Type '{0}' has suffix '{1}', which should be record types";
        private const string Description = "Use records for types that encapsulate data rather than behaviour.";

        private const string Category = DiagnosticCategory.Usage;

        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        public UseRecordTypesAnalyzer()
        {
            // Analyzers must have a public parameterless constructor to be instantiated by the framework, don't remove this.
        }

        public UseRecordTypesAnalyzer(ISettingsReader settingsReader) : this()
        {
            // This constructor is used for unit testing
            _settingsReader = settingsReader;
            _isSettingsReaderInjected = true;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(CompilationStartRegistration);
        }

        private void CompilationStartRegistration(CompilationStartAnalysisContext context)
        {
            // We don't want to reinitialize if the ISettingsReader object was injected as that means we're in a unit test
            if (_settingsReader == null || !_isSettingsReaderInjected)
            {
                _settingsReader = new EditorConfigSettingsReader(context.Options);
            }

            context.RegisterSyntaxNodeAction(
                AnalyzeClassDeclaration,
                SyntaxKind.ClassDeclaration
            );
        }

        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            var className = classDeclarationSyntax.Identifier.Text;

            if (TryMatchToForbiddenSuffix(className, context, out var forbiddenSuffix))
            {
                // This is where the diagnostic highlights the problem.
                // If we use the class declaration itself, the whole class contents will be highlighted.
                var spanLength = classDeclarationSyntax.Identifier.Span.End - classDeclarationSyntax.Keyword.SpanStart;

                // Highlight 'class XXXSuffix' and nothing else.
                var location = Location.Create(
                    classDeclarationSyntax.SyntaxTree,
                    new TextSpan(classDeclarationSyntax.Keyword.SpanStart, spanLength));
                var diagnostic = Diagnostic.Create(Rule, location, className, forbiddenSuffix);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool TryMatchToForbiddenSuffix(string className, SyntaxNodeAnalysisContext context,
            out string forbiddenSuffix)
        {
            forbiddenSuffix = null;
            var syntaxTree = context.Compilation.SyntaxTrees.FirstOrDefault();
            if (syntaxTree != null)
            {
                var forbiddenSuffixes = _settingsReader
                    .TryGetValue(syntaxTree, new SettingsKey(Id, IncludedSuffixesSetting))
                    ?.Split(',') ?? new[] { "Dto" };

                // Case-sensitive comparison
                forbiddenSuffix = forbiddenSuffixes.FirstOrDefault(suffix =>
                    className.EndsWith(suffix, StringComparison.InvariantCulture));
            }

            return forbiddenSuffix != null;
        }
    }
}