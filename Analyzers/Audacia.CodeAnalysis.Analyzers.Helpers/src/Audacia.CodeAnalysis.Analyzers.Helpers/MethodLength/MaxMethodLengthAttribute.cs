using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength
{
    public sealed class MaxMethodLengthAttribute : Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}
