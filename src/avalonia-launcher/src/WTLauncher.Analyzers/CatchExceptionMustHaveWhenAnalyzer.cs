// WT1004: catch (Exception) must have the approved when filter
// Reports diagnostic on any catch clause that catches Exception without the
// approved recoverability filter.
// Good: catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
//       catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
// Bad:  catch (Exception ex) { ... }
// Bad:  catch (Exception) { ... }
// Bad:  catch (Exception ex) when (true) { ... }   (filter present but not approved)

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

        // Approved filters: when (ExceptionFilters.IsRecoverable(x)) /
        // when (LauncherErrors.IsRecoverable(x)). Any other filter still
        // swallows fatal exceptions, so it is reported too.
        if (catchClause.Filter is { FilterExpression: { } filter } &&
            IsApprovedRecoverabilityFilter(context, filter))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, catchClause.GetLocation()));
    }

    private static bool IsApprovedRecoverabilityFilter(
        SyntaxNodeAnalysisContext context, ExpressionSyntax filterExpression)
    {
        // Form 1: when (ExceptionFilters.IsRecoverable(ex)) / LauncherErrors.IsRecoverable(ex)
        if (filterExpression is InvocationExpressionSyntax invocation &&
            IsRecoverabilityInvocation(context, invocation))
            return true;

        // Form 2: narrow type filter — when (ex is IOException or UnauthorizedAccessException).
        // The rule message advertises "or narrow to specific exception types"; constant
        // filters like `when (true)` are not narrowing and stay reported.
        return filterExpression is IsPatternExpressionSyntax isPattern &&
               ContainsOnlyTypePatterns(context, isPattern.Pattern);
    }

    private static bool IsRecoverabilityInvocation(
        SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return false;
        if (method.Name != "IsRecoverable")
            return false;

        var containingType = method.ContainingType;
        if (containingType is null)
            return false;
        if (containingType.Name is not ("ExceptionFilters" or "LauncherErrors"))
            return false;

        var ns = containingType.ContainingNamespace?.ToDisplayString();
        return ns is "WTLauncher.App.ViewModels" or "WTLauncher.Core";
    }

    /// <summary>True when the pattern tree narrows by type only (type patterns,
    /// or/and/parenthesized combinations). Unary `not` patterns and constants
    /// that are not types (e.g. `is not null`) are not type narrowing and are
    /// rejected. Roslyn parses a plain type name in `x is T` as a constant
    /// pattern — the binder resolves it; only symbols that ARE named types count.</summary>
    private static bool ContainsOnlyTypePatterns(SyntaxNodeAnalysisContext context, PatternSyntax pattern) =>
        pattern switch
        {
            TypePatternSyntax => true,
            BinaryPatternSyntax binary =>
                ContainsOnlyTypePatterns(context, binary.Left) &&
                ContainsOnlyTypePatterns(context, binary.Right),
            ParenthesizedPatternSyntax parenthesized =>
                ContainsOnlyTypePatterns(context, parenthesized.Pattern),
            ConstantPatternSyntax constant =>
                context.SemanticModel.GetSymbolInfo(constant.Expression).Symbol is INamedTypeSymbol,
            _ => false,
        };
}
