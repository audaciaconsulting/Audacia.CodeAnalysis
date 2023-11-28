using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Shared.Extensions
{
    public static class OperationExtensions
    {
        public static bool HasErrors(this IOperation operation, Compilation compilation, CancellationToken cancellationToken = default)
        {
            if (operation.Syntax == null)
            {
                return true;
            }

            SemanticModel model = compilation.GetSemanticModel(operation.Syntax.SyntaxTree);

            return model.GetDiagnostics(operation.Syntax.Span, cancellationToken)
                .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        public static bool IsForLoopVariable(this IVariableDeclaratorOperation declarator)
        {
            return declarator.Parent?.Kind == OperationKind.VariableDeclaration &&
                   declarator.Parent.Parent?.Kind == OperationKind.VariableDeclarationGroup &&
                   declarator.Parent.Parent.Parent?.Kind == OperationKind.Loop &&
                   ((ILoopOperation)declarator.Parent.Parent.Parent).LoopKind == LoopKind.For;
        }
    }
}
