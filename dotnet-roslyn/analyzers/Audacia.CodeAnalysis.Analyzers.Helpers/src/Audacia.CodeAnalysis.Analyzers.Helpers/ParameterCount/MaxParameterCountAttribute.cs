using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount
{
    /// <summary>
    /// Overrides the maximum allowed number of parameters to a method or constructor to a given value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
    public sealed class MaxParameterCountAttribute : Attribute
    {
        /// <summary>
        /// Gets the maximum number of parameters allowed.
        /// </summary>
        public int ParameterCount { get; }

        /// <summary>
        /// Gets or sets a justification explaining why the default value has been overridden.
        /// This is optional, but it can be useful if there are external constraints restricting what can be done to the code in question.
        /// </summary>
        public string Justification { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="MaxParameterCountAttribute"/>, with the number of parameters allowed set to <paramref name="parameterCount"/>.
        /// </summary>
        /// <param name="parameterCount">The number of parameters allowed in the decorated method.</param>
        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}