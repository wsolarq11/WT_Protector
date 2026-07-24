using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LayerViolationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1001";

    // Layer 0 (Core) must not reference ViewModels namespace
    private static readonly DiagnosticDescriptor CoreToViewModelsRule = new(
        DiagnosticId,
        title: "Core must not reference App.ViewModels",
        messageFormat: "Core layer ('WTLauncher.Core') must not reference 'WTLauncher.App.ViewModels'. Move the referenced type to Core or remove the using.",
        category: "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [CoreToViewModelsRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Only check Core project
            if (!compilationContext.Compilation.AssemblyName?.StartsWith("WTLauncher.Core", StringComparison.Ordinal) == true)
                return;

            compilationContext.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);
        });
    }

    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var name = usingDirective.Name?.ToString();

        if (name != null && (name.Contains("WTLauncher.App.ViewModels") || name.Contains("WTLauncher.App.Views")))
        {
            context.ReportDiagnostic(Diagnostic.Create(CoreToViewModelsRule, usingDirective.GetLocation()));
        }
    }
}
