using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.TestProjects.Extensions
{
    /// <summary>
    /// Test-specific extensions to analysis contexts.
    /// </summary>
    public static class TestAnalysisContextExtensions
    {
        private static readonly ImmutableArray<string> TestMethodAttributeNames = ImmutableArray.Create(
            "Fact",
            "Theory",
            "Xunit.Fact",
            "Xunit.Theory",
            "FactAttribute",
            "TheoryAttribute",
            "Xunit.FactAttribute",
            "Xunit.TheoryAttribute"
        );

        /// <summary>
        /// Checks whether the method is decorated with a [Fact] or [Theory] attribute, which are used to denote test methods in xUnit.
        /// </summary>
        /// <param name="context"></param>
        /// <returns><c>true</c> if the method is an xUnit test method; otherwise, <c>false</c>.</returns>
        public static bool IsXunitTestMethod(this SyntaxNodeAnalysisContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

            var methodAttributes = methodDeclarationSyntax.GetMethodAttributes();

            var isTestMethod = methodAttributes
                .Any(
                    attribute =>
                        TestMethodAttributeNames.Any(name => attribute.Equals(name))
                );

            return isTestMethod;
        }
    }
}
