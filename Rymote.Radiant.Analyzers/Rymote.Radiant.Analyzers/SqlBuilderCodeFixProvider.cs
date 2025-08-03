using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rymote.Radiant.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SqlBuilderCodeFixProvider)), Shared]
public sealed class SqlBuilderCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(SqlBuilderAnalyzer.MissingBuildCallDiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        InvocationExpressionSyntax? invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?.AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add Build() call",
                createChangedDocument: cancellationToken => AddBuildCallAsync(context.Document, invocation, cancellationToken),
                equivalenceKey: "AddBuildCall"),
            diagnostic);
    }

    private async Task<Document> AddBuildCallAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (oldRoot == null) return document;

        ExpressionSyntax currentExpression = invocation;
        while (currentExpression.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == currentExpression)
        {
            if (memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
            {
                currentExpression = parentInvocation;
            }
            else
            {
                currentExpression = memberAccess;
            }
        }

        MemberAccessExpressionSyntax buildMemberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            currentExpression,
            SyntaxFactory.IdentifierName("Build"));

        InvocationExpressionSyntax buildInvocation = SyntaxFactory.InvocationExpression(
            buildMemberAccess,
            SyntaxFactory.ArgumentList());

        SyntaxNode newRoot = oldRoot.ReplaceNode(currentExpression, buildInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
} 