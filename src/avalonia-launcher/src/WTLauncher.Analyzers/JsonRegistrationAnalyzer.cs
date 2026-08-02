// WT1006: types used with LauncherJson.Save<T>/Load<T> must be registered with
// [JsonSerializable] on LauncherJsonContext.
//
// Implementation note: registration types are resolved semantically from the
// compilation — LauncherJsonContext's [JsonSerializable(typeof(T))] attributes
// are read via GetAttributes(). In the real App build, Core's compiled
// LauncherJsonContext (source-generated partial) carries those attributes as
// assembly metadata, so no generated-file reading is required.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Scope guard: the registration contract applies to App-layer callers.
            if (!compilationContext.Compilation.AssemblyName?.StartsWith("WTLauncher.App", StringComparison.Ordinal) == true)
                return;

            var contextType = compilationContext.Compilation
                .GetTypeByMetadataName("WTLauncher.Core.LauncherJsonContext");
            if (contextType is null)
                return;

            var registered = CollectRegisteredTypes(contextType);
            if (registered.Count == 0)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                ctx => AnalyzeInvocation(ctx, registered),
                SyntaxKind.InvocationExpression);
        });
    }

    private static HashSet<ISymbol> CollectRegisteredTypes(INamedTypeSymbol contextType)
    {
        var set = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var attribute in contextType.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "System.Text.Json.Serialization.JsonSerializableAttribute")
                continue;
            if (attribute.ConstructorArguments.Length != 1)
                continue;
            if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol type)
                set.Add(type);
        }
        return set;
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, HashSet<ISymbol> registered)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return;
        if (method.Name is not ("Save" or "Load" or "LoadRequired"))
            return;
        if (method.ContainingType?.ToString() != "WTLauncher.Core.LauncherJson")
            return;
        if (method.TypeArguments.Length != 1)
            return;

        var typeArgument = method.TypeArguments[0];
        if (registered.Contains(typeArgument))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), typeArgument.Name));
    }
}
