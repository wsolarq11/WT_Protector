using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DispatchMustBeOnlyWritePathAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Dispatch must be the only write path",
        messageFormat: "Direct call to Save outside of StoreGrain.Dispatch is not allowed. " +
                       "Use StoreGrain.Dispatch instead of calling LauncherStore.Save or IStorePersistence.Save directly.",
        category: "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this calls Save on LauncherStore or IStorePersistence
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "Save") return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                var containingType = method.ContainingType?.ToString();
                if (containingType is not ("WTLauncher.Core.LauncherStore" or "WTLauncher.Core.IStorePersistence"))
                    return;

                // Check if we're inside an allowed calling context
                var enclosingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                if (enclosingMethod != null)
                {
                    var enclosingMethodName = enclosingMethod.Identifier.Text;
                    var parentType = enclosingMethod.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    var parentTypeName = parentType?.Identifier.Text;

                    // StoreGrain.Dispatch, DispatchErrorOr, or LazyInit — the designated write path
                    if (parentTypeName is "StoreGrain" &&
                        enclosingMethodName is "Dispatch" or "DispatchErrorOr" or "LazyInit")
                        return;

                    // LauncherStore.Save — the static public API facade that delegates to IStorePersistence
                    if (parentTypeName is "LauncherStore" && enclosingMethodName is "Save")
                        return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}
