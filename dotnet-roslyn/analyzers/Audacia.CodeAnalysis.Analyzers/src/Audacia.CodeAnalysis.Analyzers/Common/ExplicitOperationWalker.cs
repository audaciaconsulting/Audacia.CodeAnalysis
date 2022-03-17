using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Common
{
    /// <summary>
    /// A walker that skips compiler-generated / implicitly computed operations.
    /// </summary>
    internal abstract class ExplicitOperationWalker : OperationWalker
    {
        public override void Visit(IOperation operation)
        {
            if (!operation.IsImplicit)
            {
                base.Visit(operation);
            }
        }
    }
}
