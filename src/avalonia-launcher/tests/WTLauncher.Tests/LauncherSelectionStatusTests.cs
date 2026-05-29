using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherSelectionStatusTests
{
    [Fact]
    public void StatusTextDescribesSelectionActions()
    {
        Assert.Equal("当前没有可勾选的可见项", LauncherSelectionStatus.NoVisibleEntries);
        Assert.Equal("可见项已全部勾选", LauncherSelectionStatus.VisibleAlreadySelected);
        Assert.Equal("未选择任何启动项", LauncherSelectionStatus.NoSelectedEntries);
        Assert.Equal("当前没有已勾选项", LauncherSelectionStatus.NoSelectedEntriesToClear);
        Assert.Equal("请先勾选至少一个启动项", LauncherSelectionStatus.SelectionRequired);
        Assert.Equal("已勾选可见 2 项（已保存）", LauncherSelectionStatus.VisibleSelected(2));
        Assert.Equal("已清空 2 个勾选项（已保存）", LauncherSelectionStatus.Cleared(2));
    }

    [Fact]
    public void StatusTextDescribesBatchLaunchSelectionOutcomes()
    {
        var exception = new InvalidOperationException("boom");

        Assert.Equal("已取消: 批量启动 2 项", LauncherSelectionStatus.BatchLaunchCanceled(2));
        Assert.Equal("批量启动失败: boom", LauncherSelectionStatus.BatchLaunchFailed(exception));
    }
}
