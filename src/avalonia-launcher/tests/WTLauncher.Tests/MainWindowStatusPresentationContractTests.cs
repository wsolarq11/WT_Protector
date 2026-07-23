namespace WTLauncher.Tests;

public sealed class MainWindowStatusPresentationContractTests
{
    [Fact]
    public void MainWindow_StatusPanelKeepsAuthoritativeTextAndNonColorCue()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.View(Path.Combine("Controls", "StatusBar.axaml")));

        Assert.Contains("Classes=\"status-panel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-success=\"{Binding IsStatusSuccess}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-warning=\"{Binding IsStatusWarning}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.status-error=\"{Binding IsStatusError}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusIndicator}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StatusText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"状态信息\"", xaml, StringComparison.Ordinal);
    }
}
