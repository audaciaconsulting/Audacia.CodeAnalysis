using System;

namespace Audacia.CodeAnalysis.Analyzers.Helpers
{
    /// <summary>
    /// Attribute that, when added to a method parameter will, by convention, stop analyzers flagging possible null arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
