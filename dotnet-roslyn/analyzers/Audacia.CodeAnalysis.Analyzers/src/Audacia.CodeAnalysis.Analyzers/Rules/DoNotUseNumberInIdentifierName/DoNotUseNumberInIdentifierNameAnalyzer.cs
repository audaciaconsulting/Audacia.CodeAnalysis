using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.DoNotUseNumberInIdentifierName
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseNumberInIdentifierNameAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.DoNotUseNumberInIdentifierName;
        public const string AllowedWordsSetting = "allowed_words";

        private const string Title = "Identifier contains one or more digits in its name";
        private const string MessageFormat = "{0} '{1}' contains one or more digits in its name";
        private const string Description = "Don't include numbers in variables, parameters and type members.";

        private const string Category = DiagnosticCategory.Naming;

        private static readonly string HelpLinkUrl = HelpLinkUrlFactory.Create(Id);

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            Description,
            HelpLinkUrl);

        private static readonly ImmutableArray<SymbolKind> MemberSymbolKinds =
            ImmutableArray.Create(SymbolKind.Property, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event);

        private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private readonly Action<SymbolAnalysisContext> _analyzeNamedTypeAction;
        private readonly Action<SymbolAnalysisContext> _analyzeMemberAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeParameterAction;
        private readonly Action<OperationAnalysisContext> _analyzeLocalFunctionAction;
        private readonly Action<OperationAnalysisContext> _analyzeVariableDeclaratorAction;
        private readonly Action<OperationAnalysisContext> _analyzeTupleAction;
        private readonly Action<OperationAnalysisContext> _analyzeAnonymousObjectCreationAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeFromClauseAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeJoinClauseAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeJoinIntoClauseAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeQueryContinuationAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeLetClauseAction;
        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public DoNotUseNumberInIdentifierNameAnalyzer()
        {
            _analyzeNamedTypeAction = context => context.SkipEmptyName(AnalyzeNamedType);
            _analyzeMemberAction = context => context.SkipEmptyName(AnalyzeMember);
            _analyzeParameterAction = context => context.SkipEmptyName(AnalyzeParameter);
            _analyzeLocalFunctionAction = context => context.SkipInvalid(AnalyzeLocalFunction);
            _analyzeVariableDeclaratorAction = context => context.SkipInvalid(AnalyzeVariableDeclarator);
            _analyzeTupleAction = context => context.SkipInvalid(AnalyzeTuple);
            _analyzeAnonymousObjectCreationAction = context => context.SkipInvalid(AnalyzeAnonymousObjectCreation);
            _analyzeFromClauseAction = AnalyzeFromClause;
            _analyzeJoinClauseAction = AnalyzeJoinClause;
            _analyzeJoinIntoClauseAction = AnalyzeJoinIntoClause;
            _analyzeQueryContinuationAction = AnalyzeQueryContinuation;
            _analyzeLetClauseAction = AnalyzeLetClause;
        }

        public DoNotUseNumberInIdentifierNameAnalyzer(ISettingsReader settingsReader)
            : this()
        {
            _settingsReader = settingsReader;
            _isSettingsReaderInjected = true;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(CompilationStartRegistration);
        }

        private void CompilationStartRegistration(CompilationStartAnalysisContext context)
        {
            // We don't want to reinitialize if the ISettingsReader object was injected as that means we're in a unit test
            if (_settingsReader == null || !_isSettingsReaderInjected)
            {
                _settingsReader = new EditorConfigSettingsReader(context.Options);
            }

            RegisterForSymbols(context);
            RegisterForOperations(context);
            RegisterForSyntax(context);
        }

        private void RegisterForSymbols(CompilationStartAnalysisContext context)
        {
            context.RegisterSymbolAction(_analyzeNamedTypeAction, SymbolKind.NamedType);
            context.RegisterSymbolAction(_analyzeMemberAction, MemberSymbolKinds);
            context.RegisterSyntaxNodeAction(_analyzeParameterAction, SyntaxKind.Parameter);
        }

        private void RegisterForOperations(CompilationStartAnalysisContext context)
        {
            context.RegisterOperationAction(_analyzeLocalFunctionAction, OperationKind.LocalFunction);
            context.RegisterOperationAction(_analyzeVariableDeclaratorAction, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(_analyzeTupleAction, OperationKind.Tuple);
            context.RegisterOperationAction(_analyzeAnonymousObjectCreationAction,
                OperationKind.AnonymousObjectCreation);
        }

        private void RegisterForSyntax(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(_analyzeFromClauseAction, SyntaxKind.FromClause);
            context.RegisterSyntaxNodeAction(_analyzeJoinClauseAction, SyntaxKind.JoinClause);
            context.RegisterSyntaxNodeAction(_analyzeJoinIntoClauseAction, SyntaxKind.JoinIntoClause);
            context.RegisterSyntaxNodeAction(_analyzeQueryContinuationAction, SyntaxKind.QueryContinuation);
            context.RegisterSyntaxNodeAction(_analyzeLetClauseAction, SyntaxKind.LetClause);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.IsSynthesized())
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(type.Name, context.GetSyntaxTree()))
            {
                var diagnostic = Diagnostic.Create(Rule, type.Locations[0], type.TypeKind, type.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            ISymbol member = context.Symbol;

            if (member.IsPropertyOrEventAccessor() || member.IsOverride || member.IsSynthesized())
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(member.Name, context.GetSyntaxTree()) && !member.IsOverride && !member.IsInterfaceImplementation())
            {
                var diagnostic = Diagnostic.Create(Rule, member.Locations[0], member.GetKind(), member.Name);
                context.ReportDiagnostic(diagnostic);
            }

            ITypeSymbol memberType = member.GetSymbolType();
            AnalyzeTypeAsTuple(memberType, context.ReportDiagnostic, context.GetSyntaxTree());
        }

        private void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var localFunction = (ILocalFunctionOperation)context.Operation;

            if (ContainsDigitsNonWhitelisted(localFunction.Symbol.Name, context.GetSyntaxTree()))
            {
                var diagnostic = Diagnostic.Create(Rule, localFunction.Symbol.Locations[0],
                    localFunction.Symbol.GetKind(), localFunction.Symbol.Name);
                context.ReportDiagnostic(diagnostic);
            }

            AnalyzeTypeAsTuple(localFunction.Symbol.ReturnType, context.ReportDiagnostic, context.GetSyntaxTree());
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);

            if (parameter.IsSynthesized())
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(parameter.Name, context.GetSyntaxTree()) && !parameter.ContainingSymbol.IsOverride &&
                !parameter.IsInterfaceImplementation())
            {
                var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Kind, parameter.Name);
                context.ReportDiagnostic(diagnostic);
            }

            AnalyzeTypeAsTuple(parameter.Type, context.ReportDiagnostic, context.GetSyntaxTree());
        }

        private  void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var declarator = (IVariableDeclaratorOperation)context.Operation;
            ILocalSymbol variable = declarator.Symbol;

            if (variable.IsSynthesized())
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(variable.Name, context.GetSyntaxTree()))
            {
                var diagnostic = Diagnostic.Create(Rule, variable.Locations[0], "Variable", variable.Name);
                context.ReportDiagnostic(diagnostic);
            }

            AnalyzeTypeAsTuple(variable.Type, context.ReportDiagnostic, context.GetSyntaxTree());
        }

        private void AnalyzeTypeAsTuple(ITypeSymbol type, Action<Diagnostic> reportDiagnostic, SyntaxTree syntaxTree)
        {
            if (type.IsTupleType && type is INamedTypeSymbol tupleType)
            {
                foreach (IFieldSymbol tupleElement in tupleType.TupleElements)
                {
                    bool isDefaultTupleElement = tupleElement.IsEqualTo(tupleElement.CorrespondingTupleField);

                    if (!isDefaultTupleElement && ContainsDigitsNonWhitelisted(tupleElement.Name, syntaxTree))
                    {
                        var diagnostic = Diagnostic.Create(Rule, tupleElement.Locations[0], "Tuple element",
                            tupleElement.Name);
                        reportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private void AnalyzeTuple(OperationAnalysisContext context)
        {
            var tuple = (ITupleOperation)context.Operation;

            foreach (IOperation element in tuple.Elements)
            {
                ILocalSymbol tupleElement = TryGetTupleElement(element);

                if (tupleElement != null && ContainsDigitsNonWhitelisted(tupleElement.Name, context.GetSyntaxTree()))
                {
                    var diagnostic = Diagnostic.Create(Rule, tupleElement.Locations[0], "Tuple element",
                        tupleElement.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static ILocalSymbol TryGetTupleElement(IOperation elementOperation)
        {
            ILocalReferenceOperation localReference =
                elementOperation is IDeclarationExpressionOperation declarationExpression
                    ? declarationExpression.Expression as ILocalReferenceOperation
                    : elementOperation as ILocalReferenceOperation;

            return localReference != null && localReference.IsDeclaration ? localReference.Local : null;
        }

        private void AnalyzeAnonymousObjectCreation(OperationAnalysisContext context)
        {
            var creationExpression = (IAnonymousObjectCreationOperation)context.Operation;

            if (!creationExpression.IsImplicit)
            {
                foreach (IPropertySymbol property in creationExpression.Type.GetMembers().OfType<IPropertySymbol>())
                {
                    if (ContainsDigitsNonWhitelisted(property.Name, context.GetSyntaxTree()))
                    {
                        var diagnostic = Diagnostic.Create(Rule, property.Locations[0], "Property", property.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private void AnalyzeFromClause(SyntaxNodeAnalysisContext context)
        {
            var fromClause = (FromClauseSyntax)context.Node;
            AnalyzeRangeVariable(fromClause.Identifier, context);
        }

        private void AnalyzeJoinClause(SyntaxNodeAnalysisContext context)
        {
            var joinClause = (JoinClauseSyntax)context.Node;
            AnalyzeRangeVariable(joinClause.Identifier, context);
        }

        private void AnalyzeJoinIntoClause(SyntaxNodeAnalysisContext context)
        {
            var joinIntoClause = (JoinIntoClauseSyntax)context.Node;
            AnalyzeRangeVariable(joinIntoClause.Identifier, context);
        }

        private void AnalyzeQueryContinuation(SyntaxNodeAnalysisContext context)
        {
            var queryContinuation = (QueryContinuationSyntax)context.Node;
            AnalyzeRangeVariable(queryContinuation.Identifier, context);
        }

        private void AnalyzeLetClause(SyntaxNodeAnalysisContext context)
        {
            var letClause = (LetClauseSyntax)context.Node;
            AnalyzeRangeVariable(letClause.Identifier, context);
        }

        private void AnalyzeRangeVariable(SyntaxToken identifierToken, SyntaxNodeAnalysisContext context)
        {
            string rangeVariableName = identifierToken.ValueText;

            if (string.IsNullOrEmpty(rangeVariableName))
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(rangeVariableName, context.GetSyntaxTree()))
            {
                Location location = identifierToken.GetLocation();

                var diagnostic = Diagnostic.Create(Rule, location, "Range variable", rangeVariableName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool ContainsDigitsNonWhitelisted(string text, SyntaxTree syntaxTree)
        {
            if (ContainsDigit(text))
            {
                string newText = RemoveWordsOnWhitelist(text, syntaxTree);
                return ContainsDigit(newText);
            }

            return false;
        }

        private string RemoveWordsOnWhitelist(string text, SyntaxTree syntaxTree)
        {
            var allowedWords = _settingsReader
                .TryGetValue(syntaxTree, new SettingsKey(Id, AllowedWordsSetting))
                ?.Split(',')
                .Select(suffix => suffix.Trim()) ?? Array.Empty<string>();
            foreach (var allowedWord in allowedWords)
            {
                var indexOfAllowedWord = text.IndexOf(allowedWord, StringComparison.OrdinalIgnoreCase);
                if (indexOfAllowedWord > -1)
                {
                    // Would like to use the overload of string replace here but cannot until we're on a higher version of .NET
                    text = $"{text.Substring(0, indexOfAllowedWord)}{text.Substring(indexOfAllowedWord + allowedWord.Length, text.Length - indexOfAllowedWord - allowedWord.Length)}";
                }
            }

            return text;
        }

        private static bool ContainsDigit(string text)
        {
            return text.IndexOfAny(Digits) != -1;
        }
    }
}