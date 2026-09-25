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

        // 命令栏唯一常驻的高频按钮：启动已勾选 + 应用启动组
        foreach (var visibleAction in new[] { "BarLaunchSelected", "ApplyPresetButtonText" })
        {
            XamlContractAssertions.AssertAttribute(xaml, "Content", "{Binding " + visibleAction + "}");
        }

        // 其余低频操作一律收进 ⋯ 菜单（MenuItem Header）
        foreach (var menuAction in new[]
        {
            "BarAddEntry",
            "BarDiscover",
            "BarPreview",
            "BarSaveAsNewPreset",
            "BarUpdatePreset",
            "BarDeletePreset",
            "BarSelectAll",
            "BarClearSelection",
            "BarReload",
            "LaunchConfirmationToggleText",
            "BarExportAll",
            "BarExportSelected",
            "BarImport",
            "BarRunHistory",
            "BarCheckUpdate",
            "BarCommands",
            "BarAddress",
            "BarAccentColor",
        })
        {
            XamlContractAssertions.AssertAttribute(xaml, "Header", "{Binding " + menuAction + "}");
        }

        // 所有命令仍在（无论常驻按钮还是菜单项）
        foreach (var command in new[]
        {
            "LaunchSelectedCommand",
            "ApplyPresetCommand",
            "AddEntryCommand",
            "DiscoverAgentsCommand",
            "PreviewSelectedPresetCommand",
            "SaveSelectionAsPresetCommand",
            "UpdateSelectedPresetCommand",
            "DeleteSelectedPresetCommand",
            "SelectVisibleCommand",
            "ClearSelectionCommand",
            "RefreshCommand",
            "ExportAllConfigCommand",
            "ExportSelectedConfigCommand",
            "ImportConfigCommand",
            "OpenRunHistoryCommand",
            "CheckForUpdatesCommand",
            "OpenCommandManagementCommand",
            "OpenDirectoryManagementCommand",
            "OpenAccentColorCommand",
        })
        {
            XamlContractAssertions.AssertAttribute(xaml, "Command", "{Binding " + command + "}");
        }

        // 旧布局的三处 secondary 按钮必须消失
        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "全选可见"));
        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "清空"));
        XamlContractAssertions.AssertNoElement(xaml, "Button", ("Classes", "secondary"), ("Content", "重新加载"));
        Assert.Contains("OnOverflowMenuClicked", codeBehind, StringComparison.Ordinal);
        Assert.Contains("menu.Open(control);", codeBehind, StringComparison.Ordinal);
        Assert.Contains("e.Handled = true;", codeBehind, StringComparison.Ordinal);

        // 命令栏控件自动化名称
        foreach (var automationBinding in new[] { "BarSearch", "BarSort", "BarView", "BarCardsGroup", "BarPresetGroup", "BarManageAutomation" })
            XamlContractAssertions.AssertAttribute(xaml, "AutomationProperties.Name", "{Binding " + automationBinding + "}");

        XamlContractAssertions.AssertAttribute(entriesXaml, "AutomationProperties.Name", "{Binding LaunchLabel}");
        XamlContractAssertions.AssertAttribute(entriesXaml, "AutomationProperties.Name", "{Binding MoreLabel}");
        XamlContractAssertions.AssertNoAttribute(entriesXaml, "Content", "编辑");
        XamlContractAssertions.AssertNoAttribute(entriesXaml, "Content", "删除");
    }
}
