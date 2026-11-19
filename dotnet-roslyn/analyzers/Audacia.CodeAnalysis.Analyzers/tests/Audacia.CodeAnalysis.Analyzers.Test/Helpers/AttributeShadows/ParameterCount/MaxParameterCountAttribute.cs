using System;

namespace Audacia.CodeAnalysis.Analyzers.Test.Helpers.AttributeShadows.ParameterCount;

/// <summary>
/// Overrides the maximum allowed number of parameters to a method or constructor to a given value.
/// </summary>
/// <remarks>
/// <para>This is strictly for testing the identical attribute in Audacia.CodeAnalysis.Analyzers.Helpers.ParameterCount without a pacakge reference.</para>
/// </remarks>
/// <remarks>
/// <para>Initializes an instance of <see cref="MaxParameterCountAttribute"/>, with the number of parameters allowed set to <paramref name="parameterCount"/>.</para>
/// </remarks>
/// <param name="parameterCount">The number of parameters allowed in the decorated method.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
public sealed class MaxParameterCountAttribute(int parameterCount) : Attribute
{
    public int ParameterCount { get; } = parameterCount;

    public string Justification { get; set; }
}