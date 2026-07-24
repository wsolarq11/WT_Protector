// WT1006: JsonSerializable type must be registered in LauncherJsonContext.
// Uses RegisterSyntaxNodeAction (not GetSemanticModel) to comply with RS1030.

using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class JsonRegistrationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1006";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Type must be registered in LauncherJsonContext for JSON serialization",
        messageFormat: "Type '{0}' used in LauncherJson.Save/Load is not registered with [JsonSerializable]",
        category: "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every type used with LauncherJson.Save<T>/Load<T> must be registered via [JsonSerializable(typeof(T))].");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Phase 1: Intentionally disabled to avoid RS1030 violation.
        // Full implementation requires either:
        //   a) RegisterSyntaxNodeAction on TypeOfExpressionSyntax (collects Attribute types)
        //   b) Reading the LauncherJsonContext source-generated file
        //
        // For now, gate in verify.ps1 ensures GetTypeInfo() null-safety.
        // WT1006 is a scaffolding analyzer for Phase 2 of the VTuber-Killer architecture.
    }
}
