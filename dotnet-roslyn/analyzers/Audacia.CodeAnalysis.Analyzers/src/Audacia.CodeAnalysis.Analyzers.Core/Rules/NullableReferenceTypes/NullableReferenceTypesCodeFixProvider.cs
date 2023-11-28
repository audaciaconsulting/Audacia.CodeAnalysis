using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Audacia.CodeAnalysis.Analyzers.Shared.Common;

namespace Audacia.CodeAnalysis.Analyzers.Rules.NullableReferenceTypes
{
    /// <summary>
    /// The code fix provider for the <see cref="NullableReferenceTypesAnalyzer"/> rule.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableReferenceTypesCodeFixProvider)), Shared]
    public class NullableReferenceTypesCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// The diagnostic ID(s) that this class provides fixes for.
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.NullableReferenceTypesEnabled);

        /// <summary>
        /// Allow this fixer to be used concurrently on multiple projects within a solution.
        /// </summary>
        /// <returns></returns>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        /// Registers the fix for the related <see cref="NullableReferenceTypesAnalyzer"/>.
        /// </summary>
        /// <param name="context">The code fix context.</param>
        /// <returns>A <see cref="Task"/> representing the fix for this diagnostic.</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .Project
                .GetCompilationAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var existingCompilationOptions = root?.Options;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Enable nullable reference types",
                    _ => EnableNullableReferenceTypes(context.Document.Project, existingCompilationOptions),
                    DiagnosticId.NullableReferenceTypesEnabled),
                diagnostic);
        }

        /// <summary>
        /// Enables nullable reference types on the given <paramref name="project"/>.
        /// </summary>
        /// <param name="project">The csproj on which nullable reference types should be enabled.</param>
        /// <param name="existingCompilationOptions">The existing compilation options.</param>
        /// <returns>The <paramref name="project"/> with nullable types enabled.</returns>
        private static Task<Solution> EnableNullableReferenceTypes(
            Project project,
            CompilationOptions existingCompilationOptions)
        {
            if (existingCompilationOptions.NullableContextOptions == NullableContextOptions.Disable)
            {
                var newCompilationOptions = new CSharpCompilationOptions(
                existingCompilationOptions.OutputKind,
                nullableContextOptions: NullableContextOptions.Enable);

                project = project.WithCompilationOptions(newCompilationOptions);
            }

            return Task.FromResult(project.Solution);
        }
    }
}
