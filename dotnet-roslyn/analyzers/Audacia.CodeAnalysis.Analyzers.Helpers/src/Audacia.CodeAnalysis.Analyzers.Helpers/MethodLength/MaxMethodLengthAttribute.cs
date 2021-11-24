using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength
{
    /// <summary>
    /// Overrides the maximum allowed number of statements in a method to a given value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
    public sealed class MaxMethodLengthAttribute : Attribute
    {
        /// <summary>
        /// Gets the maximum number of statements allowed in the method.
        /// </summary>
        public int StatementCount { get; }

        /// <summary>
        /// Gets or sets a justification explaining why the default value has been overridden.
        /// This is optional, but it can be useful if there are external constraints restricting what can be done to the code in question.
        /// </summary>
        public string Justification { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="MaxMethodLengthAttribute"/>, with the number of statements allowed set to <paramref name="statementCount"/>.
        /// </summary>
        /// <param name="statementCount">The number of statements allowed in the decorated method.</param>
        public MaxMethodLengthAttribute(int statementCount)
        {
            StatementCount = statementCount;
        }
    }
}
