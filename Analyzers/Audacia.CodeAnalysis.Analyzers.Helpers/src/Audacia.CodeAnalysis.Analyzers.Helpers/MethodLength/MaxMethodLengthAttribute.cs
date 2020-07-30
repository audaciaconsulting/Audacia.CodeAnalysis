using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength
{
    /// <summary>
    /// Overrides the maximum allowed number of statements in a method to a given value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
    public sealed class MaxMethodLengthAttribute : Attribute
    {
        public int StatementCount { get; }

        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}
