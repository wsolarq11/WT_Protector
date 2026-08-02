// Code fix provider for WT1004: adds a when filter to catch (Exception) clauses.
// Uses DocumentEditor for structured edits as required by project standards.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace WTLauncher.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CatchExceptionMustHaveWhenCodeFixProvider))]
[Shared]
public class CatchExceptionMustHaveWhenCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var catchClause = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add approved when filter",
                createChangedDocument: ct => AddWhenFilterAsync(
                    context.Document, catchClause, ct),
                equivalenceKey: nameof(CatchExceptionMustHaveWhenCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> AddWhenFilterAsync(
        Document document, CatchClauseSyntax catchClause, CancellationToken ct)
    {
        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

        // Determine which filter method to use based on what's available
        // in the current compilation.
        var compilation = await document.Project
            .GetCompilationAsync(ct).ConfigureAwait(false);
        var useExceptionFilters = compilation?
            .GetTypeByMetadataName("WTLauncher.App.ViewModels.ExceptionFilters") != null;
        var filterMethod = useExceptionFilters
            ? "ExceptionFilters.IsRecoverable"
            : "LauncherErrors.IsRecoverable";

        // Get or create the exception variable name.
        var declaration = catchClause.Declaration;
        var variableName = declaration?.Identifier.Text;
        if (string.IsNullOrEmpty(variableName))
        {
            variableName = "ex";

            // When the catch declaration has no variable name (e.g. "catch (Exception)"),
            // we need to add one. Clone the declaration with an identifier.
            if (declaration != null)
            {
                var newDeclaration = declaration.WithIdentifier(
                    SyntaxFactory.Identifier(variableName));
                editor.ReplaceNode(declaration, newDeclaration);
            }
        }

        // Build: when (ExceptionFilters.IsRecoverable(variableName))
        var filterExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression($"{filterMethod}({variableName})"));
        var whenFilter = SyntaxFactory.CatchFilterClause(filterExpression);

        var newCatchClause = catchClause.WithFilter(whenFilter);
        editor.ReplaceNode(catchClause, newCatchClause);

        // Add the required using directive if the namespace is not already imported.
        var requiredNamespace = useExceptionFilters
            ? "WTLauncher.App.ViewModels"
            : "WTLauncher.Core";

        if (!IsNamespaceImported(await document.GetSyntaxRootAsync(ct).ConfigureAwait(false),
                requiredNamespace))
        {
            var compilationUnit = await document.GetSyntaxRootAsync(ct)
                .ConfigureAwait(false);
            if (compilationUnit is CompilationUnitSyntax cus)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName(requiredNamespace))
                    .NormalizeWhitespace();

                var lastUsing = cus.Usings.LastOrDefault();
                if (lastUsing != null)
                {
                    editor.InsertAfter(lastUsing, usingDirective);
                }
                else if (cus.Members.Count > 0)
                {
                    editor.InsertBefore(cus.Members[0], usingDirective);
                }
            }
        }

        return editor.GetChangedDocument();
    }

    /// <summary>Check if a namespace is already imported in the document.</summary>
    private static bool IsNamespaceImported(SyntaxNode? root, string namespaceName)
    {
        if (root is not CompilationUnitSyntax compilationUnit) return false;

        foreach (var usingDirective in compilationUnit.Usings)
        {
            var name = usingDirective.Name?.ToString();
            if (name == namespaceName) return true;
        }
        return false;
    }
}
