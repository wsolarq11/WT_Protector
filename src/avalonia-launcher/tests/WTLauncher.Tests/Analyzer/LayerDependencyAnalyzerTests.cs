using WTLauncher.Analyzers;

namespace WTLauncher.Tests.Analyzer;

public sealed class LayerDependencyAnalyzerTests
{
    [Fact]
    public void PresetService_ReferencingViews_ReportsDiagnostic()
    {
        var source = """
            namespace WTLauncher.App.Features.Presets
            {
                public class PresetService
                {
                    public void M() => _ = new WTLauncher.App.Views.SomeWindow();
                }
            }
            namespace WTLauncher.App.Views
            {
                public class SomeWindow { }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        var wt1008 = Assert.Single(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
        Assert.Contains("WTLauncher.App.Views", wt1008.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void CoreType_ReferencingViewModels_ReportsDiagnostic()
    {
        var source = """
            namespace WTLauncher.Core
            {
                public class SomeCoreType
                {
                    public void M() => _ = new WTLauncher.App.ViewModels.SomeViewModel();
                }
            }
            namespace WTLauncher.App.ViewModels
            {
                public class SomeViewModel { }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        var wt1008 = Assert.Single(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
        Assert.Contains("WTLauncher.App.ViewModels", wt1008.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModel_ReferencingSystemIoFileViaShortName_ReportsDiagnostic()
    {
        var source = """
            namespace WTLauncher.App.ViewModels
            {
                using System.IO;
                public class SomeViewModel
                {
                    public void M() => File.WriteAllText("x", "y");
                }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        var wt1008 = Assert.Single(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
        Assert.Contains("System.IO.File", wt1008.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void CoreLaunchArgumentBuilder_ReferencingSystemIoFile_IsExempt()
    {
        var source = """
            namespace WTLauncher.Core
            {
                using System.IO;
                public class LaunchArgumentBuilder
                {
                    public bool M() => File.Exists("x");
                }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
    }

    [Fact]
    public void StoreGrain_ReferencingLauncherStore_ReportsDiagnostic()
    {
        var source = """
            namespace WTLauncher.App.Infrastructure
            {
                public class StoreGrain
                {
                    public void M() => WTLauncher.Core.LauncherStore.Save();
                }
            }
            namespace WTLauncher.Core
            {
                public static class LauncherStore
                {
                    public static void Save() { }
                }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        var wt1008 = Assert.Single(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
        Assert.Contains("WTLauncher.Core.LauncherStore", wt1008.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void UnrelatedDependency_ReportsNothing()
    {
        var source = """
            namespace WTLauncher.App.ViewModels
            {
                public class SomeViewModel
                {
                    public string M() => "ok";
                }
            }
            """;

        var diagnostics = AnalyzerTestHelper.Run<LayerDependencyAnalyzer>(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == LayerDependencyAnalyzer.DiagnosticId);
    }
}
