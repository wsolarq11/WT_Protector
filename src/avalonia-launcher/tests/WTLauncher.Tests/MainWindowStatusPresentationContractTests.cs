namespace WTLauncher.Tests;

public sealed class MainWindowStatusPresentationContractTests
{
    [Fact]
    public void MainWindow_StatusPanelKeepsAuthoritativeTextAndNonColorCue()
    {
        // The status panel was extracted to a separate StatusBar.axaml UserControl
        // as part of view modularization (CQ-04). Check both files for completeness.
        var mainWindowXaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);
        var statusBarXaml = File.ReadAllText(TestRepositoryPaths.StatusBarXaml);

        // MainWindow must include the StatusBar control
        Assert.Contains("StatusBar", mainWindowXaml, StringComparison.Ordinal);

        // StatusBar.axaml must contain the binding contract
        Assert.Contains("Classes=\"status-panel\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-success=\"{Binding IsStatusSuccess}\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-warning=\"{Binding IsStatusWarning}\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-error=\"{Binding IsStatusError}\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusIndicator}\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusText}\"", statusBarXaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"状态信息\"", statusBarXaml, StringComparison.Ordinal);
    }
}
