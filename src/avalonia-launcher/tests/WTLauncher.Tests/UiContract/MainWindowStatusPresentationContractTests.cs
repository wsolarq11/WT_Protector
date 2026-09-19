using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class MainWindowStatusPresentationContractTests
{
    [Fact]
    public void MainWindow_StatusPanelKeepsAuthoritativeTextAndNonColorCue()
    {
        var mainWindowXaml = XamlContractAssertions.Load(TestRepositoryPaths.MainWindowXaml);
        var statusBarXaml = XamlContractAssertions.Load(TestRepositoryPaths.StatusBarXaml);

        XamlContractAssertions.AssertElement(mainWindowXaml, "StatusBar");

        XamlContractAssertions.AssertElement(statusBarXaml, "Border", ("Classes", "status-panel"));
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Classes.status-success", "{Binding IsStatusSuccess}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Classes.status-warning", "{Binding IsStatusWarning}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Classes.status-error", "{Binding IsStatusError}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Data", "{StaticResource IconStatusCheck}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Data", "{StaticResource IconStatusAlert}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Data", "{StaticResource IconStatusX}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Data", "{StaticResource IconStatusCircle}");
        XamlContractAssertions.AssertAttribute(statusBarXaml, "Text", "{Binding StatusText}");
        XamlContractAssertions.AssertNoAttribute(statusBarXaml, "AutomationProperties.Name", "状态信息");
        XamlContractAssertions.AssertNoAttribute(statusBarXaml, "AutomationProperties.Name", "状态级别");

        var statusBarCodeBehind = File.ReadAllText(TestRepositoryPaths.StatusBarCodeBehind);
        Assert.Contains("status.message", statusBarCodeBehind, StringComparison.Ordinal);
        Assert.Contains("status.level", statusBarCodeBehind, StringComparison.Ordinal);
    }
}
