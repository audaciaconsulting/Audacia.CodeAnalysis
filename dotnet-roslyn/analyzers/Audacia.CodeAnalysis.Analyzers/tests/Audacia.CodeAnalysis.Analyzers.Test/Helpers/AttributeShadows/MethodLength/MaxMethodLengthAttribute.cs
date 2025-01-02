using System;

namespace Audacia.CodeAnalysis.Analyzers.Test.Helpers.AttributeShadows.MethodLength;

/// <summary>
/// Overrides the maximum allowed number of statements in a method to a given value.
/// </summary>
/// <remarks>
/// This is strictly for testing the identical attribute in Audacia.CodeAnalysis.Analyzers.Helpers.MethodLength without a package reference.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
public sealed class MaxMethodLengthAttribute : Attribute
{
    public int StatementCount { get; }

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
