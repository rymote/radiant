using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rymote.Radiant.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SqlBuilderAnalyzer : DiagnosticAnalyzer
{
    public const string MissingFromClauseDiagnosticId = "RR0001";
    public const string DuplicateClauseDiagnosticId = "RR0002";
    public const string InvalidOperatorDiagnosticId = "RR0003";
    public const string MissingBuildCallDiagnosticId = "RR0004";

    private static readonly DiagnosticDescriptor MissingFromClauseRule = new DiagnosticDescriptor(
        MissingFromClauseDiagnosticId,
        "Missing FROM clause",
        "SelectBuilder must have a FROM clause before WHERE, JOIN, or GROUP BY",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A FROM clause is required before using WHERE, JOIN, or GROUP BY clauses.");

    private static readonly DiagnosticDescriptor DuplicateClauseRule = new DiagnosticDescriptor(
        DuplicateClauseDiagnosticId,
        "Duplicate clause detected",
        "Duplicate {0} clause detected. This may overwrite the previous clause.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple calls to the same clause method may overwrite previous values.");

    private static readonly DiagnosticDescriptor InvalidOperatorRule = new DiagnosticDescriptor(
        InvalidOperatorDiagnosticId,
        "Invalid SQL operator",
        "'{0}' is not a valid SQL operator. Use operators like '=', '!=', '>', '<', 'LIKE', 'IN', '= ANY', etc.",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The operator provided is not a valid SQL operator.");

    private static readonly DiagnosticDescriptor MissingBuildCallRule = new DiagnosticDescriptor(
        MissingBuildCallDiagnosticId,
        "Missing Build() call",
        "Query builder chain should end with Build() to generate QueryCommand",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Query builders should call Build() to generate the final QueryCommand.");

    private static readonly ImmutableArray<string> ValidOperators = ImmutableArray.Create(
        "=", "!=", "<>", ">", ">=", "<", "<=", "LIKE", "ILIKE", "NOT LIKE", "IN", "NOT IN", 
        "IS", "IS NOT", "EXISTS", "NOT EXISTS", "BETWEEN", "NOT BETWEEN", "= ANY"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(MissingFromClauseRule, DuplicateClauseRule, InvalidOperatorRule, MissingBuildCallRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            IMethodSymbol? methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null) return;

            string containingTypeName = methodSymbol.ContainingType.Name;
            
            if (containingTypeName == "SelectBuilder" || containingTypeName == "UpdateBuilder" || 
                containingTypeName == "DeleteBuilder" || containingTypeName == "InsertBuilder")
            {
                AnalyzeBuilderUsage(context, invocation, methodSymbol);
            }
        }
    }

    private static void AnalyzeBuilderUsage(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
    {
        string methodName = methodSymbol.Name;

        if (methodName == "Where" && invocation.ArgumentList.Arguments.Count >= 3)
        {
            ArgumentSyntax operatorArgument = invocation.ArgumentList.Arguments[1];
            if (operatorArgument.Expression is LiteralExpressionSyntax literal && 
                literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                string operatorValue = literal.Token.ValueText;
                if (!ValidOperators.Contains(operatorValue.ToUpperInvariant()))
                {
                    Diagnostic diagnostic = Diagnostic.Create(InvalidOperatorRule, operatorArgument.GetLocation(), operatorValue);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        ExpressionSyntax? currentExpression = invocation.Parent as ExpressionSyntax;
        while (currentExpression != null && (currentExpression is MemberAccessExpressionSyntax || currentExpression is InvocationExpressionSyntax))
        {
            currentExpression = currentExpression.Parent as ExpressionSyntax;
        }

        if (currentExpression != null && !IsFollowedByBuild(invocation))
        {
            SyntaxNode? parent = invocation.Parent;
            while (parent != null && !(parent is StatementSyntax))
            {
                parent = parent.Parent;
            }

            if (parent is ExpressionStatementSyntax)
            {
                Diagnostic diagnostic = Diagnostic.Create(MissingBuildCallRule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsFollowedByBuild(SyntaxNode node)
    {
        SyntaxNode? current = node;
        while (current != null)
        {
            if (current.Parent is MemberAccessExpressionSyntax memberAccess && 
                memberAccess.Name.Identifier.Text == "Build")
            {
                return true;
            }
            
            if (!(current.Parent is MemberAccessExpressionSyntax || current.Parent is InvocationExpressionSyntax))
            {
                break;
            }
            
            current = current.Parent;
        }
        return false;
    }
} 