using System;
using System.Linq.Expressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Moq;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Minimal stand-in for the Moq API, compiled into the test assembly (which test code snippets reference) so the Moq package isn't needed.
/// The analyzer only checks the namespace and method name of verification calls.
/// </summary>
/// <remarks>
/// <para>This allows tests to check Mock-related analyzers, without actually having to have the Moq package installed.</para>
/// <para>It only works in this project and namespace, and will cause problems if removed without updating tests that use "Moq.Mock".</para>
/// </remarks>
public class Mock<T> where T : class
{
    public T Object => default;

    public ISetup Setup(Expression<Action<T>> expression) => default;

    public void Verify(Expression<Action<T>> expression, Times times) { }

    public void VerifyAll() { }

    public void VerifyNoOtherCalls() { }

    public void VerifyGet<TProperty>(Expression<Func<T, TProperty>> expression, Times times) { }

    public void VerifySet(Action<T> setterExpression, Times times) { }

    public void VerifyAdd(Action<T> addExpression, Times times) { }

    public void VerifyRemove(Action<T> removeExpression, Times times) { }
}

public interface ISetup
{
    void Verifiable();
}

public struct Times
{
    public static Times Once() => default;
}

public static class It
{
    public static TValue IsAny<TValue>() => default;
}