using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LayerViolationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1001";

    // Layer 0 (Core) must not reference ViewModels or Views namespace
    private static readonly DiagnosticDescriptor CoreToViewModelsRule = new(
        DiagnosticId,
        title: "Core must not reference App.ViewModels or App.Views",
        messageFormat: "Core layer ('WTLauncher.Core') must not reference 'WTLauncher.App.ViewModels' or 'WTLauncher.App.Views'. " +
                       "Move the referenced type to Core or remove the reference.",
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

            // Catch using directives that import forbidden namespaces
            compilationContext.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);

            // Catch inline fully-qualified type references (e.g., var x = new WTLauncher.App.ViewModels.Foo())
            compilationContext.RegisterSyntaxNodeAction(AnalyzeQualifiedName, SyntaxKind.QualifiedName);
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

    /// <summary>
    /// Catches fully-qualified type references like
    /// <c>WTLauncher.App.ViewModels.SomeViewModel</c> that bypass using directives.
    /// Qualified names that belong to a using directive are skipped — the
    /// using-directive check already reports those, and reporting twice would
    /// duplicate diagnostics on a single line.
    /// </summary>
    private static void AnalyzeQualifiedName(SyntaxNodeAnalysisContext context)
    {
        var qualifiedName = (QualifiedNameSyntax)context.Node;
        if (qualifiedName.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null)
            return;

        // Nested qualified names (A.B.C contains A.B) all match the string check;
        // report only the outermost node so a single reference yields one diagnostic.
        if (qualifiedName.Parent is QualifiedNameSyntax)
            return;

        var name = qualifiedName.ToString();

        if (name.Contains("WTLauncher.App.ViewModels") || name.Contains("WTLauncher.App.Views"))
        {
            context.ReportDiagnostic(Diagnostic.Create(CoreToViewModelsRule, qualifiedName.GetLocation()));
        }
    }
}
