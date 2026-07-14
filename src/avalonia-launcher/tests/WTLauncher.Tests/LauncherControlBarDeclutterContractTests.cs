namespace WTLauncher.Tests;

public sealed class LauncherControlBarDeclutterContractTests
{
    [Fact]
    public void ControlBar_KeepsPrimaryActionsVisibleAndMovesMaintenanceToMenus()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var codeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml.cs"));
        var entriesXaml = File.ReadAllText(TestRepositoryPaths.View("LauncherEntriesView.axaml"));

        foreach (var visibleAction in new[]
        {
            "Content=\"一键启动已勾选\"",
            "Content=\"新增按钮\"",
            "Content=\"发现\"",
            "Content=\"选择与刷新\"",
            "Content=\"管理\"",
            "Content=\"启动组维护\"",
        })
        {
            Assert.Contains(visibleAction, xaml, StringComparison.Ordinal);
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
            Assert.Contains($"Command=\"{{Binding {command}}}\"", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("<Button Classes=\"secondary\" Content=\"全选可见\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Button Classes=\"secondary\" Content=\"清空\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Button Classes=\"secondary\" Content=\"重新加载\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OnOverflowMenuClicked", codeBehind, StringComparison.Ordinal);
        Assert.Contains("menu.Open(control);", codeBehind, StringComparison.Ordinal);
        Assert.Contains("e.Handled = true;", codeBehind, StringComparison.Ordinal);

        foreach (var automationName in new[] { "选择与刷新菜单", "管理菜单", "启动组维护菜单" })
            Assert.Contains($"automation:AutomationProperties.Name=\"{automationName}\"", xaml, StringComparison.Ordinal);

        Assert.Contains("AutomationProperties.Name=\"启动\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"更多操作\"", entriesXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"编辑\"", entriesXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"删除\"", entriesXaml, StringComparison.Ordinal);
    }
}
