using System;
using System.Collections.Generic;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Shared.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.AspNetCore.Extensions
{
    /// <summary>
    /// ASP.NET Core controller-specific extensions.
    /// </summary>
    public static class ControllerExtensions
    {
        private static readonly IReadOnlyList<string> ControllerActionAttributeNames = new List<string>
        {
            "HttpDelete",
            "HttpGet",
            "HttpHead",
            "HttpOptions",
            "HttpPatch",
            "HttpPost",
            "HttpPut"
        };

        private static readonly IReadOnlyList<string> ControllerBaseTypeNames = new List<string>
        {
            "Controller",
            "ControllerBase"
        };

        /// <summary>
        /// Returns true if method is representing a http action (e.g. get/post/delete/put etc)
        /// </summary>
        public static bool IsControllerAction(this SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)nodeAnalysisContext.Node;

            var methodAttributes = methodDeclarationSyntax.GetMethodAttributes();

            var isControllerAction = methodAttributes
                .Any(
                    attribute =>
                        ControllerActionAttributeNames.Any(name => attribute.StartsWith(name))
                );

            var containingTypeIsControllerType = IsControllerBaseType(nodeAnalysisContext);

            var isPublicMethod = IsPublic(methodDeclarationSyntax);

            return (isControllerAction || containingTypeIsControllerType) && isPublicMethod;
        }

        /// <summary>
        /// Returns true if method is representing a http action (e.g. get/post/delete/put etc)
        /// </summary>
        public static bool IsControllerAction(this IMethodSymbol method)
        {
            var attributes = method.GetAttributes().Select(attribute => attribute.AttributeClass.Name.ToString());

            var isControllerAction = attributes.Any(attribute =>
                ControllerActionAttributeNames.Any(name =>
                    attribute.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)));

            return isControllerAction;
        }

        /// <summary>
        /// Checks whether the method has a public modifier.
        /// </summary>
        private static bool IsPublic(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax
                .Modifiers
                .Any(
                    modifier =>
                        modifier.ValueText.Equals("public", StringComparison.InvariantCultureIgnoreCase)
                );
        }

        /// <summary>
        /// Determines whether a methods containing type is a controller base type
        /// </summary>
        private static bool IsControllerBaseType(SyntaxNodeAnalysisContext nodeAnalysisContext)
        {
            var classBaseType = nodeAnalysisContext
                .SemanticModel
                .GetDeclaredSymbol(nodeAnalysisContext.Node)
                .ContainingType
                .BaseType;

            if (classBaseType == null)
            {
                return false;
            }

            return ControllerBaseTypeNames
                .Any(
                    controllerBaseTypeName => classBaseType
                        .Name
                        .Equals(
                            controllerBaseTypeName,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                );
        }
    }
}
