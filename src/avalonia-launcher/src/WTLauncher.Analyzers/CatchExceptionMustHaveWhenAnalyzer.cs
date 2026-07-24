// WT1004: catch (Exception) must have a when filter
// Reports diagnostic on any catch clause that catches Exception without a when clause.
// Good: catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
// Bad:  catch (Exception ex) { ... }
// Bad:  catch (Exception) { ... }

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CatchExceptionMustHaveWhenAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Catch Exception must have a when filter",
        messageFormat: "Bare 'catch (Exception)' without 'when' filter is not allowed. Use 'catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))' or narrow to specific exception types.",
        category: "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Bare catch (Exception) swallows fatal exceptions like OutOfMemoryException and StackOverflowException.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCatch, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatch(SyntaxNodeAnalysisContext context)
    {
        var catchClause = (CatchClauseSyntax)context.Node;

        // Check if this catch clause catches "Exception"
        var declaration = catchClause.Declaration;

        // Case 1: bare `catch { }` or `catch` without type declaration
        if (declaration == null)
        {
            // Bare catch without any type — also not allowed (same reasoning)
            context.ReportDiagnostic(Diagnostic.Create(Rule, catchClause.GetLocation()));
            return;
        }

        var caughtType = context.SemanticModel.GetTypeInfo(declaration.Type).Type;
        var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
        if (caughtType == null || exceptionType == null) return;
        if (!SymbolEqualityComparer.Default.Equals(caughtType, exceptionType)) return;

        // If it has a when filter, it's allowed
        if (catchClause.Filter != null) return;

        // Report diagnostic
        context.ReportDiagnostic(Diagnostic.Create(Rule, catchClause.GetLocation()));
    }
}
