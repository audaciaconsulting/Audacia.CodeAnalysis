using System;

namespace Audacia.CodeAnalysis.Analyzers.Test.Helpers.AttributeShadows.MethodLength;

/// <summary>
/// Overrides the maximum allowed number of statements in a method to a given value.
/// </summary>
/// <remarks>
/// <para>This is strictly for testing the identical attribute in Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength without a package reference.</para>
/// </remarks>
/// <remarks>
/// <para>Initializes an instance of <see cref="MaxMethodLengthAttribute"/>, with the number of statements allowed set to <paramref name="statementCount"/>.</para>
/// </remarks>
/// <param name="statementCount">The number of statements allowed in the decorated method.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
public sealed class MaxMethodLengthAttribute(int statementCount) : Attribute
{
    public int StatementCount { get; } = statementCount;

    public string Justification { get; set; }
}
