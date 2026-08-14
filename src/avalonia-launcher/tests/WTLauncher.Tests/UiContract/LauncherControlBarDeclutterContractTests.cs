using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class LauncherControlBarDeclutterContractTests
{
    [Fact]
    public void ControlBar_KeepsPrimaryActionsVisibleAndMovesMaintenanceToMenus()
    {
        var xaml = XamlContractAssertions.Load(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var codeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml.cs"));
        var entriesXaml = XamlContractAssertions.Load(TestRepositoryPaths.View("LauncherEntriesView.axaml"));

        foreach (var visibleAction in new[]
        {
            "一键启动已勾选",
            "新增按钮",
            "发现",
            "选择与刷新",
            "管理",
            "启动组维护",
        })
        {
            XamlContractAssertions.AssertAttribute(xaml, "Content", visibleAction);
        }

        foreach (var command in new[]
        {
            "SelectVisibleCommand",
            "ClearSelectionCommand",
            "RefreshCommand",
            "OpenCommandManagementCommand",
            "OpenDirectoryManagementCommand",
            "SaveSelectionAsPresetCommand",
            "UpdateSelectedPresetCommand",
            "DeleteSelectedPresetCommand",
        })
        {
            XamlContractAssertions.AssertAttribute(xaml, "Command", "{Binding " + command + "}");
        }

        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "全选可见"));
        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "清空"));
        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "重新加载"));
        Assert.Contains("OnOverflowMenuClicked", codeBehind, StringComparison.Ordinal);
        Assert.Contains("menu.Open(control);", codeBehind, StringComparison.Ordinal);
        Assert.Contains("e.Handled = true;", codeBehind, StringComparison.Ordinal);

        foreach (var automationName in new[] { "选择与刷新菜单", "管理菜单", "启动组维护菜单" })
            XamlContractAssertions.AssertAttribute(xaml, "AutomationProperties.Name", automationName);

        XamlContractAssertions.AssertAttribute(entriesXaml, "AutomationProperties.Name", "启动");
        XamlContractAssertions.AssertAttribute(entriesXaml, "AutomationProperties.Name", "更多操作");
        XamlContractAssertions.AssertNoAttribute(entriesXaml, "Content", "编辑");
        XamlContractAssertions.AssertNoAttribute(entriesXaml, "Content", "删除");
    }
}
