using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

// ponytail: collections in test infrastructure — perf-irrelevant in tests.
#pragma warning disable CA1861

namespace WTLauncher.Tests;

/// <summary>
/// Helper for running Roslyn analyzers against test source code.
///
/// The helper creates a C# compilation that includes shim source files.
/// To avoid type conflicts with the real project assemblies, the default
/// metadata references exclude assemblies whose name starts with "WTLauncher".
///
/// Usage:
/// <code>
/// var diags = AnalyzerTestHelper.Run&lt;CatchExceptionMustHaveWhenAnalyzer&gt;(
///     source, assemblyName: "WTLauncher.Tests", shimSource1, shimSource2);
/// Assert.Single(diags.Where(d => d.Id == "WT1004"));
/// </code>
/// </summary>
internal static class AnalyzerTestHelper
{
    /// <summary>
    /// Framework metadata references — excludes WTLauncher project assemblies
    /// to avoid type conflicts with shim types defined in test source files.
    /// </summary>
    private static readonly ImmutableArray<MetadataReference> FrameworkReferences =
        GetFrameworkReferences();

    private static ImmutableArray<MetadataReference> GetFrameworkReferences()
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (IsUsableFrameworkAssembly(assembly))
            {
                var reference = MetadataReference.CreateFromFile(assembly.Location);
                if (reference != null)
                    builder.Add(reference);
            }
        }
        return builder.ToImmutable();
    }

    private static bool IsUsableFrameworkAssembly(Assembly a)
    {
        if (a.IsDynamic) return false;
        try
        {
            var name = a.GetName().Name;
            if (name == null || string.IsNullOrEmpty(a.Location)) return false;

            // Exclude the project's own assemblies so shim source types
            // can be defined without CS0436 conflicts.
            if (name.StartsWith("WTLauncher.", StringComparison.Ordinal)) return false;
            if (name.StartsWith("WTLauncher.Tests", StringComparison.Ordinal)) return false;

            return true;
        }
#pragma warning disable CA1031, WT1004 // Test infrastructure — assembly reflection can fail in many ways
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }

    /// <summary>Run a single analyzer against source code with optional
    /// additional source files (for shim types) and a configurable assembly
    /// name (for scope-guard testing).</summary>
    internal static IReadOnlyList<Diagnostic> Run<TAnalyzer>(
        string source,
        string? assemblyName = null,
        params string[] additionalSources)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var trees = new List<SyntaxTree>(1 + additionalSources.Length)
        {
            CSharpSyntaxTree.ParseText(source),
        };
        foreach (var src in additionalSources)
            trees.Add(CSharpSyntaxTree.ParseText(src));

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName ?? "WTLauncher.Tests",
            syntaxTrees: trees,
            references: FrameworkReferences,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new TAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));

        return withAnalyzers
            .GetAllDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
