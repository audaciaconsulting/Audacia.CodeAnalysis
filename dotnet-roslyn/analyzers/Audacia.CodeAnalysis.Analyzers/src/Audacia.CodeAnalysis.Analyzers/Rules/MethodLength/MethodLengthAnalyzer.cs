using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Audacia.CodeAnalysis.Analyzers.Common;
using Audacia.CodeAnalysis.Analyzers.Extensions;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Audacia.CodeAnalysis.Analyzers.Rules.MethodLength
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MethodLengthAnalyzer : DiagnosticAnalyzer
    {
        public const int DefaultMaxStatementCount = 10;

        public const string Id = DiagnosticId.MethodLength;

        private const string Title = "Member or local function contains too many statements";

        private const string MessageFormat =
            "{0} '{1}' contains {2} statements, which exceeds the maximum of {3} statements.";

        private const string Description =
            "Methods should not exceed a predefined number of statements. You can configure the maximum number of allowed statements globally in the .editorconfig file, or locally using the [MaxMethodLength] attribute.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            DiagnosticCategory.Maintainability,
            DiagnosticSeverity.Warning,
            true,
            Description);

        private static readonly Action<CompilationStartAnalysisContext> RegisterCompilationStartAction =
            RegisterCompilationStart;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(RegisterCompilationStartAction);
        }

        private static void RegisterCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var settingsReader = new EditorConfigSettingsReader(startContext.Options);
            startContext.RegisterCodeBlockAction(actionContext => AnalyzeCodeBlock(actionContext, settingsReader));
        }

        private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context,
            EditorConfigSettingsReader settingsReader)
        {
            if (ShouldSkipSymbol(context.OwningSymbol))
            {
                return;
            }

            var maxStatementCount = GetMaxStatementCount(context, settingsReader);
            var statementWalker = new StatementWalker(context.SemanticModel, context.CancellationToken);
            statementWalker.Visit(context.CodeBlock);

            if (statementWalker.StatementCount > maxStatementCount)
            {
                ReportAtContainingSymbol(statementWalker.StatementCount, maxStatementCount, context);
            }
        }

        private static bool ShouldSkipSymbol(ISymbol symbol) =>
            symbol is INamedTypeSymbol ||
            symbol.Kind == SymbolKind.Namespace ||
            symbol.IsSynthesized() ||
            symbol.IsPrimaryConstructor();
        
        private static int GetMaxStatementCount(CodeBlockAnalysisContext context,
            EditorConfigSettingsReader settingsReader)
        {
            int maxStatementCount = DefaultMaxStatementCount;
            var attributes = context.OwningSymbol.GetAttributes();
            var maxLengthAttribute =
                attributes.FirstOrDefault(att => att.AttributeClass.Name == "MaxMethodLengthAttribute");
            if (maxLengthAttribute != null)
            {
                var maxLengthArgument = maxLengthAttribute.ConstructorArguments.First();
                maxStatementCount = (int)maxLengthArgument.Value;
            }
            else
            {
                // Look up in .editorconfig
                var configValue = settingsReader.TryGetInt(context.CodeBlock.SyntaxTree,
                    new SettingsKey(Id, "max_statement_count"));
                maxStatementCount = configValue ?? maxStatementCount;
            }

            return maxStatementCount;
        }

        private static void ReportAtContainingSymbol(int statementCount, int maxStatementCount,
            CodeBlockAnalysisContext context)
        {
            string kind = GetMemberKind(context.OwningSymbol, context.CancellationToken);
            string memberName = context.OwningSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
            var location = GetMemberLocation(context.OwningSymbol, context.SemanticModel, context.CancellationToken);

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, kind, memberName, statementCount,
                maxStatementCount));
        }

        private static string GetMemberKind(ISymbol member, CancellationToken cancellationToken)
        {
            foreach (SyntaxNode syntax in member.DeclaringSyntaxReferences.Select(reference =>
                         reference.GetSyntax(cancellationToken)))
            {
                if (syntax is VariableDeclaratorSyntax || syntax is PropertyDeclarationSyntax)
                {
                    return "Initializer for";
                }
            }

            return member.GetKind();
        }

        private static Location GetMemberLocation(ISymbol member, SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            foreach (var arrowExpressionClause in member.DeclaringSyntaxReferences
                         .Select(reference => reference.GetSyntax(cancellationToken))
                         .OfType<ArrowExpressionClauseSyntax>())
            {
                var parentSymbol = semanticModel.GetDeclaredSymbol(arrowExpressionClause.Parent);

                if (parentSymbol != null && parentSymbol.Locations.Any())
                {
                    return parentSymbol.Locations[0];
                }
            }

            return member.Locations[0];
        }

        private sealed class StatementWalker : CSharpSyntaxWalker
        {
            private CancellationToken _cancellationToken;

            private bool _nullChecksFinished;

            private readonly SemanticModel _semanticModel;

            public int StatementCount { get; private set; }

            public StatementWalker(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public override void Visit(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (IsStatement(node))
                {
                    StatementCount++;
                }

                base.Visit(node);
            }

            private bool IsStatement(SyntaxNode node)
            {
                return !node.IsMissing &&
                       node is StatementSyntax statement &&
                       !IsExcludedStatement(statement) &&
                       !IsLoggingStatement(node, _semanticModel);
            }

            private static bool IsLoggingStatement(SyntaxNode node, SemanticModel semanticModel)
            {
                // Check if the node is an invocation expression
                if (node is ExpressionStatementSyntax expressionStatement &&
                    expressionStatement.Expression is InvocationExpressionSyntax invocation)
                {
                    // Check if the expression being invoked is a member access (like "logger.Info")
                    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type?.Name;
                        // Check if the name of the method being accessed starts with "Log"
                        return type == "ILogger" && memberAccess.Name.Identifier.Text.StartsWith("Log");
                    }
                }

                // If none of the above checks passed, then it's not a logging statement
                return false;
            }

            private bool IsExcludedStatement(StatementSyntax node)
            {
                var isExcludedNodeType = node is BlockSyntax ||
                                         node is LabeledStatementSyntax ||
                                         node is LocalFunctionStatementSyntax;
                if (isExcludedNodeType)
                {
                    return true;
                }

                if (!_nullChecksFinished)
                {
                    // Argument null checks will be at the top of a method, so once we're past them we can stop checking
                    var isArgumentNullCheck = node.IsArgumentNullCheck();
                    if (!isArgumentNullCheck)
                    {
                        _nullChecksFinished = true;
                    }

                    return isArgumentNullCheck;
                }

                return false;
            }
        }
    }
}