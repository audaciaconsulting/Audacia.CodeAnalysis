namespace Audacia.CodeAnalysis.Analyzers.Shared.Common
{
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
            switch (diagnosticId)
            {
                case DiagnosticId.FieldWithUnderscore:
                    return $"{helpLinkUrlPrefix}private-fields-should-be-prefixed-with-an-underscore";
                case DiagnosticId.MagicNumber:
                    return $"{helpLinkUrlPrefix}variable-declarations-should-not-use-a-magic-number";
                case DiagnosticId.MethodLength:
                    return $"{helpLinkUrlPrefix}methods-should-not-exceed-a-predefined-number-of-statements";
                case DiagnosticId.ParameterCount:
                    return $"{helpLinkUrlPrefix}dont-declare-signatures-with-more-than-a-predefined-number-of-parameters";
                case DiagnosticId.NoAbbreviations:
                    return $"{helpLinkUrlPrefix}dont-use-abbreviations";
                case DiagnosticId.AsyncSuffix:
                    return $"{helpLinkUrlPrefix}asynchronous-method-name-is-not-suffixed-with-async";
                case DiagnosticId.IncludeBraces:
                    return $"{helpLinkUrlPrefix}code-block-does-not-have-braces";
                case DiagnosticId.ThenByDescendingAfterOrderBy:
                    return $"{helpLinkUrlPrefix}thenbydescending-instead-of-orderbydescending-if-follows-orderby-or-orderbydescending-statement";
                case DiagnosticId.ControllerActionProducesResponseType:
                    return $"{helpLinkUrlPrefix}controller-actions-have-producesresponsetype-attribute-when-return-type-is-not-typedresults";
                case DiagnosticId.OverloadShouldCallOtherOverload:
                    return $"{helpLinkUrlPrefix}method-overload-should-call-another-overload";
                case DiagnosticId.NullableReferenceTypesEnabled:
                    return $"{helpLinkUrlPrefix}nullable-reference-types-enabled";
                case DiagnosticId.NestedControlStatements:
                    return $"{helpLinkUrlPrefix}dont-nest-too-many-control-statements";
                case DiagnosticId.MaximumWhereClauses:
                    return $"{helpLinkUrlPrefix}dont-pass-predicates-into-where-methods-with-too-many-clauses";
                case DiagnosticId.UseRecordTypes:
                    return $"{helpLinkUrlPrefix}use-record-types";
                case DiagnosticId.DoNotUseNumberInIdentifierName:
                    return $"{helpLinkUrlPrefix}do-not-include-numbers-in-identifier-name";
                case DiagnosticId.DoNotUseProducesResponseTypeWithTypedResults:
                    return $"{helpLinkUrlPrefix}controller-action-has-producesresponsetype-attribute-when-return-type-is-typedresults";
                case DiagnosticId.UseTypedResultsInsteadOfIActionResult:
                    return $"{helpLinkUrlPrefix}controller-action-should-return-typedresults-instead-of-iactionresults";                
                case DiagnosticId.SuppressionRequiresJustification:
                    return $"{helpLinkUrlPrefix}code-analysis-suppression-attribute-requires-justification";
                case DiagnosticId.UseInitForRecordSetters:
                    return $"{helpLinkUrlPrefix}record-properties-should-be-init-only";
                case DiagnosticId.AvoidBooleanParameters:
                    return $"{helpLinkUrlPrefix}avoid-signatures-that-take-a-bool-parameter-but-exclude-positional-syntax-records";
                case DiagnosticId.AssertionScopeForMultipleAssertions:
                    return $"{helpLinkUrlPrefix}use-an-assertion-scope-with-more-than-two-assertions-in-a-test-method";
                case DiagnosticId.AssertionRequired:
                    return $"{helpLinkUrlPrefix}test-methods-must-contain-at-least-one-assertion";
                case DiagnosticId.AssertionReasonMustBeProvided:
                    return $"{helpLinkUrlPrefix}assertions-must-contain-a-reason-where-supported";
                case DiagnosticId.TestNamesMustBeConsistent:
                    return $"{helpLinkUrlPrefix}test-method-names-must-be-consistent";
                case DiagnosticId.LogMessagesNamedPropertiesPascalCase:
                    return $"{helpLinkUrlPrefix}named-properties-within-log-messages-should-use-pascalcase";
                case DiagnosticId.LogMessagesNoDuplicateParameters:
                    return $"{helpLinkUrlPrefix}named-properties-within-log-messages-should-not-be-duplicated";
                case DiagnosticId.LogMessagesNoExceptionsAsTemplateParameters:
                    return $"{helpLinkUrlPrefix}exceptions-should-not-be-used-in-log-message-templates";
                case DiagnosticId.LogMessagesNamedPropertiesMustBeUsed:
                    return $"{helpLinkUrlPrefix}named-properties-should-be-used-in-logging-messages";
                case DiagnosticId.ApplicationLogicShouldBeSignposted:
                    return $"{helpLinkUrlPrefix}application-logic-should-be-signposted";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(diagnosticId),
                        $"No help link has been set up for diagnostic id {diagnosticId}");

            }
        }
    }
}