namespace Audacia.CodeAnalysis.Analyzers.Shared.Common;

/// <summary>
/// Class to construct help link URLs for our diagnostics.
/// </summary>
public static class HelpLinkUrlFactory
{
    /// <summary>
    /// All of our analyzers are documented at the same URL, with a different anchor per diagnostic.
    /// </summary>
    private const string ReadmeUrl = "https://github.com/audaciaconsulting/Audacia.CodeAnalysis/tree/master/dotnet-roslyn/analyzers/Audacia.CodeAnalysis.Analyzers";

    public static string Create(string diagnosticId)
    {
        if (diagnosticId == null)
        {
            throw new ArgumentNullException(nameof(diagnosticId));
        }

        var helpLinkUrlPrefix = $"{ReadmeUrl}#{diagnosticId.ToLower()}---";

        return diagnosticId switch
        {
            DiagnosticId.FieldWithUnderscore
                => $"{helpLinkUrlPrefix}private-fields-should-be-prefixed-with-an-underscore",
            DiagnosticId.MagicNumber
                => $"{helpLinkUrlPrefix}variable-declarations-should-not-use-a-magic-number",
            DiagnosticId.MethodLength
                => $"{helpLinkUrlPrefix}methods-should-not-exceed-a-predefined-number-of-statements",
            DiagnosticId.ParameterCount
                => $"{helpLinkUrlPrefix}dont-declare-signatures-with-more-than-a-predefined-number-of-parameters",
            DiagnosticId.NoAbbreviations
                => $"{helpLinkUrlPrefix}dont-use-abbreviations",
            DiagnosticId.AsyncSuffix
                => $"{helpLinkUrlPrefix}asynchronous-method-name-is-not-suffixed-with-async",
            DiagnosticId.IncludeBraces
                => $"{helpLinkUrlPrefix}code-block-does-not-have-braces",
            DiagnosticId.ThenByDescendingAfterOrderBy
                => $"{helpLinkUrlPrefix}thenbydescending-instead-of-orderbydescending-if-follows-orderby-or-orderbydescending-statement",
            DiagnosticId.ControllerActionProducesResponseType
                => $"{helpLinkUrlPrefix}controller-actions-have-producesresponsetype-attribute-when-return-type-is-not-typedresults",
            DiagnosticId.OverloadShouldCallOtherOverload
                => $"{helpLinkUrlPrefix}method-overload-should-call-another-overload",
            DiagnosticId.NullableReferenceTypesEnabled
                => $"{helpLinkUrlPrefix}nullable-reference-types-enabled",
            DiagnosticId.NestedControlStatements
                => $"{helpLinkUrlPrefix}dont-nest-too-many-control-statements",
            DiagnosticId.MaximumWhereClauses
                => $"{helpLinkUrlPrefix}dont-pass-predicates-into-where-methods-with-too-many-clauses",
            DiagnosticId.UseRecordTypes
                => $"{helpLinkUrlPrefix}use-record-types",
            DiagnosticId.DoNotUseNumberInIdentifierName
                => $"{helpLinkUrlPrefix}do-not-include-numbers-in-identifier-name",
            DiagnosticId.DoNotUseProducesResponseTypeWithTypedResults
                => $"{helpLinkUrlPrefix}controller-action-has-producesresponsetype-attribute-when-return-type-is-typedresults",
            DiagnosticId.UseTypedResultsInsteadOfIActionResult
                => $"{helpLinkUrlPrefix}controller-action-should-return-typedresults-instead-of-iactionresults",
            DiagnosticId.SuppressionRequiresJustification
                => $"{helpLinkUrlPrefix}code-analysis-suppression-attribute-requires-justification",
            DiagnosticId.UseInitForRecordSetters
                => $"{helpLinkUrlPrefix}record-properties-should-be-init-only",
            DiagnosticId.AvoidBooleanParameters
                => $"{helpLinkUrlPrefix}avoid-signatures-that-take-a-bool-parameter-but-exclude-positional-syntax-records",
            DiagnosticId.AssertionScopeForMultipleAssertions
                => $"{helpLinkUrlPrefix}use-an-assertion-scope-with-more-than-two-assertions-in-a-test-method",
            DiagnosticId.AssertionRequired
                => $"{helpLinkUrlPrefix}test-methods-must-contain-at-least-one-assertion",
            DiagnosticId.AssertionReasonMustBeProvided
                => $"{helpLinkUrlPrefix}assertions-must-contain-a-reason-where-supported",
            DiagnosticId.TestNamesMustBeConsistent
                => $"{helpLinkUrlPrefix}test-method-names-must-be-consistent",
            DiagnosticId.LogMessagesNamedPropertiesPascalCase
                => $"{helpLinkUrlPrefix}named-properties-within-log-messages-should-use-pascalcase",
            DiagnosticId.LogMessagesNoDuplicateParameters
                => $"{helpLinkUrlPrefix}named-properties-within-log-messages-should-not-be-duplicated",
            DiagnosticId.LogMessagesNoExceptionsAsTemplateParameters
                => $"{helpLinkUrlPrefix}exceptions-should-not-be-used-in-log-message-templates",
            DiagnosticId.LogMessagesNamedPropertiesMustBeUsed
                => $"{helpLinkUrlPrefix}named-properties-should-be-used-in-logging-messages",
            DiagnosticId.ApplicationLogicShouldBeSignposted
                => $"{helpLinkUrlPrefix}application-logic-should-be-signposted",
            DiagnosticId.LogMessageMinimumLength
                => $"{helpLinkUrlPrefix}log-messages-should-be-long-enough-to-be-meaningful",
            _ => throw new ArgumentOutOfRangeException(
                nameof(diagnosticId),
                $"No help link has been set up for diagnostic id {diagnosticId}"),
        };
    }
}