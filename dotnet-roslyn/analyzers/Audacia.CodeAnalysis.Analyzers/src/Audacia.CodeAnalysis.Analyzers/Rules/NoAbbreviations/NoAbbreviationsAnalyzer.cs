using System;
using System.Collections.Immutable;
using System.Linq;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Audacia.CodeAnalysis.Analyzers.Rules.NoAbbreviations
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoAbbreviationsAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = DiagnosticId.NoAbbreviations;
        public const string ExcludeLambdasSetting = "exclude_lambdas";
        public const string AllowedLoopVariablesSetting = "allowed_loop_variables";

        private const string Title = "Identifier contains an abbreviation or is too short";
        private const string MessageFormat = "{0} '{1}' should have a more descriptive name";
        private const string Description = "Don't use abbreviations.";

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

        private static readonly ImmutableArray<string> DisallowedAbbreviations = ImmutableArray.Create("Btn", "Ctrl", "Frm", "Chk", "Cmb",
            "Ctx", "Dg", "Pnl", "Dlg", "Ex", "Lbl", "Txt", "Mnu", "Prg", "Rb", "Cnt", "Tv", "Ddl", "Fld", "Lnk", "Img", "Lit",
            "Vw", "Gv", "Dts", "Rpt", "Vld", "Pwd", "Ctl", "Tm", "Mgr", "Flt", "Len", "Idx", "Str");

        private readonly Action<SymbolAnalysisContext> _analyzeNamedTypeAction;
        private readonly Action<SymbolAnalysisContext> _analyzeMemberAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeParameterAction;
        private readonly Action<OperationAnalysisContext> _analyzeLocalFunctionAction;
        private readonly Action<OperationAnalysisContext> _analyzeVariableDeclaratorAction;
        private readonly Action<OperationAnalysisContext> _analyzeTupleAction;
        private readonly Action<OperationAnalysisContext> _analyzeAnonymousObjectCreationAction;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeFromClauseAction = AnalyzeFromClause;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeJoinClauseAction = AnalyzeJoinClause;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeJoinIntoClauseAction = AnalyzeJoinIntoClause;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeQueryContinuationAction = AnalyzeQueryContinuation;
        private readonly Action<SyntaxNodeAnalysisContext> _analyzeLetClauseAction = AnalyzeLetClause;
        private readonly bool _isSettingsReaderInjected;
        private ISettingsReader _settingsReader;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public NoAbbreviationsAnalyzer()
        {
            _analyzeNamedTypeAction = context => context.SkipEmptyName(AnalyzeNamedType);
            _analyzeMemberAction = context => context.SkipEmptyName(AnalyzeMember);
            _analyzeParameterAction = context => context.SkipEmptyName(AnalyzeParameter);
            _analyzeLocalFunctionAction = context => context.SkipInvalid(AnalyzeLocalFunction);
            _analyzeVariableDeclaratorAction = context => context.SkipInvalid(AnalyzeVariableDeclarator);
            _analyzeTupleAction = context => context.SkipInvalid(AnalyzeTuple);
            _analyzeAnonymousObjectCreationAction = context => context.SkipInvalid(AnalyzeAnonymousObjectCreation);
        }

        public NoAbbreviationsAnalyzer(ISettingsReader settingsReader)
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
            context.RegisterOperationAction(_analyzeAnonymousObjectCreationAction, OperationKind.AnonymousObjectCreation);
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

            if (IsDisallowedOrSingleLetter(type.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.TypeKind, type.Name));
            }
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            ISymbol member = context.Symbol;

            if (member.IsPropertyOrEventAccessor() || member.IsOverride || member.IsSynthesized())
            {
                return;
            }

            if (IsDisallowedOrSingleLetter(member.Name) && !member.IsInterfaceImplementation())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member.GetKind(), member.Name));
            }

            ITypeSymbol memberType = member.GetSymbolType();
            AnalyzeTypeAsTuple(memberType, context.ReportDiagnostic);
        }

        private void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var localFunction = (ILocalFunctionOperation)context.Operation;

            if (IsDisallowedOrSingleLetter(localFunction.Symbol.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, localFunction.Symbol.Locations[0],
                    localFunction.Symbol.GetKind(), localFunction.Symbol.Name));
            }

            AnalyzeTypeAsTuple(localFunction.Symbol.ReturnType, context.ReportDiagnostic);
        }

        private void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            if (parameter.ContainingSymbol.IsOverride || parameter.IsSynthesized())
            {
                return;
            }

            var syntaxTree = context.Compilation.SyntaxTrees.FirstOrDefault();
            if (syntaxTree != null)
            {
                var excludeLambdasSetting = _settingsReader.TryGetBool(syntaxTree, new SettingsKey(Id, ExcludeLambdasSetting));
                if (excludeLambdasSetting == true &&
                    parameter.IsLambdaExpressionParameter())
                {
                    return;
                }
            }

            if (IsDisallowedOrSingleLetter(parameter.Name) && !parameter.IsInterfaceImplementation())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Kind, parameter.Name));
            }

            AnalyzeTypeAsTuple(parameter.Type, context.ReportDiagnostic);
        }

        private void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var declarator = (IVariableDeclaratorOperation)context.Operation;
            ILocalSymbol variable = declarator.Symbol;

            if (!string.IsNullOrWhiteSpace(variable.Name) && !variable.IsSynthesized())
            {
                if (IsDisallowedOrSingleLetter(variable.Name))
                {
                    // Check if it's a for loop with an allowed abbreviation, e.g. "i"
                    if (declarator.IsForLoopVariable() &&
                        IsAllowedLoopVariableName(context, variable))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Rule, variable.Locations[0], "Variable", variable.Name));
                }
            }

            AnalyzeTypeAsTuple(variable.Type, context.ReportDiagnostic);
        }

        private bool IsAllowedLoopVariableName(OperationAnalysisContext context, ILocalSymbol variable)
        {
            var syntaxTree = context.Compilation.SyntaxTrees.FirstOrDefault();
            if (syntaxTree != null)
            {
                var rawAllowedLoopVariables = _settingsReader.TryGetValue(syntaxTree, new SettingsKey(Id, AllowedLoopVariablesSetting));
                if (!string.IsNullOrEmpty(rawAllowedLoopVariables))
                {
                    var allowedLoopVariables = rawAllowedLoopVariables.Split(',');
                    if (allowedLoopVariables.Any(var => var.Trim() == variable.Name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AnalyzeTypeAsTuple(ITypeSymbol type, Action<Diagnostic> reportDiagnostic)
        {
            if (type.IsTupleType && type is INamedTypeSymbol tupleType)
            {
                foreach (IFieldSymbol tupleElement in tupleType.TupleElements)
                {
                    bool isDefaultTupleElement = tupleElement.IsEqualTo(tupleElement.CorrespondingTupleField);

                    if (!isDefaultTupleElement && IsDisallowedOrSingleLetter(tupleElement.Name))
                    {
                        reportDiagnostic(Diagnostic.Create(Rule, tupleElement.Locations[0], "Tuple element", tupleElement.Name));
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

                if (tupleElement != null && IsDisallowedOrSingleLetter(tupleElement.Name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, tupleElement.Locations[0], "Tuple element",
                        tupleElement.Name));
                }
            }
        }

        private static ILocalSymbol TryGetTupleElement(IOperation elementOperation)
        {
            ILocalReferenceOperation localReference = elementOperation is IDeclarationExpressionOperation declarationExpression
                ? declarationExpression.Expression as ILocalReferenceOperation
                : elementOperation as ILocalReferenceOperation;

            return localReference != null && localReference.IsDeclaration ? localReference.Local : null;
        }

        private static void AnalyzeAnonymousObjectCreation(OperationAnalysisContext context)
        {
            var creationExpression = (IAnonymousObjectCreationOperation)context.Operation;

            if (!creationExpression.IsImplicit)
            {
                foreach (IPropertySymbol property in creationExpression.Type.GetMembers().OfType<IPropertySymbol>())
                {
                    if (IsDisallowedOrSingleLetter(property.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, property.Locations[0], "Property", property.Name));
                    }
                }
            }
        }

        private static void AnalyzeFromClause(SyntaxNodeAnalysisContext context)
        {
            var fromClause = (FromClauseSyntax)context.Node;
            AnalyzeRangeVariable(fromClause.Identifier, context);
        }

        private static void AnalyzeJoinClause(SyntaxNodeAnalysisContext context)
        {
            var joinClause = (JoinClauseSyntax)context.Node;
            AnalyzeRangeVariable(joinClause.Identifier, context);
        }

        private static void AnalyzeJoinIntoClause(SyntaxNodeAnalysisContext context)
        {
            var joinIntoClause = (JoinIntoClauseSyntax)context.Node;
            AnalyzeRangeVariable(joinIntoClause.Identifier, context);
        }

        private static void AnalyzeQueryContinuation(SyntaxNodeAnalysisContext context)
        {
            var queryContinuation = (QueryContinuationSyntax)context.Node;
            AnalyzeRangeVariable(queryContinuation.Identifier, context);
        }

        private static void AnalyzeLetClause(SyntaxNodeAnalysisContext context)
        {
            var letClause = (LetClauseSyntax)context.Node;
            AnalyzeRangeVariable(letClause.Identifier, context);
        }

        private static void AnalyzeRangeVariable(SyntaxToken identifierToken, SyntaxNodeAnalysisContext context)
        {
            string rangeVariableName = identifierToken.ValueText;

            if (!string.IsNullOrEmpty(rangeVariableName) && IsDisallowedOrSingleLetter(rangeVariableName))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, identifierToken.GetLocation(), "Range variable",
                    rangeVariableName));
            }
        }

        private static bool IsDisallowedOrSingleLetter(string name)
        {
            return IsDisallowed(name) || IsSingleLetter(name);
        }

        private static bool IsDisallowed(string name)
        {
            return DisallowedAbbreviations.Any(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSingleLetter(string name)
        {
            return name.Length == 1 && char.IsLetter(name[0]);
        }
    }
}
