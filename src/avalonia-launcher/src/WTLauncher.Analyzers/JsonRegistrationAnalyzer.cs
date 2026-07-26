// WT1006: JsonSerializable type must be registered in LauncherJsonContext.
//
// OBSOLETE — This analyzer was a Phase-1 scaffolding placeholder and is not
// fully implemented. The planned approach (RegisterSyntaxNodeAction on
// TypeOfExpressionSyntax) would need to cross-reference against source-generated
// [JsonSerializable] attributes on LauncherJsonContext, which requires reading
// generated files from the obj/ directory — a fragile design.
//
// Alternative verification:
//   The gate (verify.ps1) ensures GetTypeInfo() null-safety at runtime.
//   AOT compilation catches missing [JsonSerializable] at publish time
//   (IL linker throws for unregistered types).
//   If static analysis is still desired, implement a separate tool that
//   inspects the compiled assembly's System.Text.Json.JsonSerializerOptions
//   metadata instead of a Roslyn analyzer.

using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[Obsolete("WT1006 is a scaffolding analyzer. " +
    "Use runtime null-safety checks and AOT compilation validation instead.")]
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
        // OBSOLETE — intentionally disabled. No syntax/compilation actions registered.
        // See file header for alternative verification methods.
    }
}
