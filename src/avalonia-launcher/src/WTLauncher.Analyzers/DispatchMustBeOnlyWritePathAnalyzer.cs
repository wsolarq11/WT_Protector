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
        messageFormat: "Direct call to 'LauncherStore.Save' outside of 'AppStore.Dispatch' is not allowed. Route all writes through AppStore.",
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

        // Check if this calls LauncherStore.Save
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName != "Save") return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                var containingType = method.ContainingType?.ToString();
                if (containingType != "WTLauncher.Core.LauncherStore") return;

                // Check if we're inside AppStore.Dispatch, DispatchAsync, or DispatchErrorOr
                var enclosingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                if (enclosingMethod != null)
                {
                    var methodName2 = enclosingMethod.Identifier.Text;
                    if (methodName2 is "Dispatch" or "DispatchAsync" or "DispatchErrorOr")
                        return; // allowed
                }

                // Also check if we're inside LauncherStore.Save itself (call to LauncherJson.Save)
                if (enclosingMethod != null && containingType == "WTLauncher.Core.LauncherStore" && methodName == "Save")
                {
                    // LauncherStore.Save calling LauncherJson.Save is allowed
                    var parentType = enclosingMethod.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    if (parentType?.Identifier.Text == "LauncherStore") return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}
