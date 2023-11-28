namespace Audacia.CodeAnalysis.Analyzers.Shared.Common
{
    /// <summary>
    /// Contains reusable meta data about diagnostic rules.
    /// </summary>
    public static class DiagnosticMessages
    {
        /// <summary>
        /// Messages for the nullable reference type rule.
        /// </summary>
        public static class NullableReferenceTypes
        {
            /// <summary>
            /// The nullable reference type analyzer title.
            /// </summary>
            public const string Title = "Nullable reference types are not enabled";

            /// <summary>
            /// The nullable reference type analyzer message format.
            /// </summary>
            public const string MessageFormat = "Nullable reference types should be enabled.";

            /// <summary>
            /// The nullable reference type analyzer description.
            /// </summary>
            public const string Description = "Nullable reference types should be enabled.";
        }
    }
}
