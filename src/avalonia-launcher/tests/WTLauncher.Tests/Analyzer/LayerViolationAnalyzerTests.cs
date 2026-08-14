using WTLauncher.Analyzers;

// ponytail: inline arrays in test assertions — perf-irrelevant in tests.
#pragma warning disable CA1861

using WTLauncher.Tests;
namespace WTLauncher.Tests.Analyzer;

/// <summary>Tests for <see cref="LayerViolationAnalyzer"/> (WT1001).</summary>
[Trait("Category", TestCategories.Analyzer)]
public sealed class LayerViolationAnalyzerTests
{
    [Fact]
    public void UsingForbiddenViewModelNamespace_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.App.ViewModels;

public class C { }
";
        // Scope guard: only Core project is checked
        var diags = AnalyzerTestHelper.Run<LayerViolationAnalyzer>(
            source, assemblyName: "WTLauncher.Core");
        var wt1001 = Assert.Single(diags, d => d.Id == LayerViolationAnalyzer.DiagnosticId);
        Assert.Contains("ViewModels", wt1001.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void UsingForbiddenViewsNamespace_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.App.Views;

public class C { }
";
        var diags = AnalyzerTestHelper.Run<LayerViolationAnalyzer>(
            source, assemblyName: "WTLauncher.Core");
        var wt1001 = Assert.Single(diags, d => d.Id == LayerViolationAnalyzer.DiagnosticId);
        Assert.Contains("Views", wt1001.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void UsingAllowedCoreNamespace_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C { }
";
        var diags = AnalyzerTestHelper.Run<LayerViolationAnalyzer>(
            source, assemblyName: "WTLauncher.Core");
        Assert.DoesNotContain(diags,
            d => d.Id == LayerViolationAnalyzer.DiagnosticId);
    }

    [Fact]
    public void QualifiedNameInForbiddenNamespace_ReportsDiagnostic()
    {
        const string source = @"
public class C {
    public void M() {
        var x = typeof(WTLauncher.App.ViewModels.SomeViewModel);
    }
}
";
        var diags = AnalyzerTestHelper.Run<LayerViolationAnalyzer>(
            source, assemblyName: "WTLauncher.Core");
        // The type doesn't resolve in the Core compilation, so it uses
        // the fallback name-matching path which should still catch it.
        var wt1001 = Assert.Single(diags, d => d.Id == LayerViolationAnalyzer.DiagnosticId);
        Assert.Contains("ViewModels", wt1001.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void NotCoreAssembly_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.App.ViewModels;

public class C { }
";
        // Not in Core assembly — scope guard skips
        var diags = AnalyzerTestHelper.Run<LayerViolationAnalyzer>(
            source, assemblyName: "WTLauncher.App");
        Assert.DoesNotContain(diags,
            d => d.Id == LayerViolationAnalyzer.DiagnosticId);
    }
}
