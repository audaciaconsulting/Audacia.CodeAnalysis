using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount
{
    /// <summary>
    /// Overrides the maximum allowed number of parameters to a method or constructor to a given value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class MaxParameterCountAttribute : Attribute
    {
        public int ParameterCount { get; }

        public MaxParameterCountAttribute(int parameterCount)
        {
            ParameterCount = parameterCount;
        }
    }
}
