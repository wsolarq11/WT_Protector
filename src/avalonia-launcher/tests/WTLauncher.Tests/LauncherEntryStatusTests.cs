using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherEntryStatusTests
{
    [Fact]
    public void StatusTextDescribesCanceledEntryActions()
    {
        Assert.Equal("已取消: 新增按钮", LauncherEntryStatus.AddCanceled);
        Assert.Equal("已取消: 编辑 One", LauncherEntryStatus.EditCanceled("One"));
        Assert.Equal("已取消: 删除 One", LauncherEntryStatus.DeleteCanceled("One"));
    }

    [Fact]
    public void StatusTextDescribesEntryDialogFailures()
    {
        var exception = new InvalidOperationException("dialog unavailable");

        Assert.Equal("新增按钮对话框失败: dialog unavailable", LauncherEntryStatus.AddDialogFailed(exception));
        Assert.Equal("编辑 One 对话框失败: dialog unavailable", LauncherEntryStatus.EditDialogFailed("One", exception));
        Assert.Equal("删除确认失败: One — dialog unavailable", LauncherEntryStatus.DeleteConfirmationFailed("One", exception));
    }

    [Fact]
    public void StatusTextDescribesSavedEntryActions()
    {
        Assert.Equal("已新增按钮: One（已保存）", LauncherEntryStatus.Added("One"));
        Assert.Equal("已更新按钮: One（已保存）", LauncherEntryStatus.Updated("One"));
        Assert.Equal("已删除按钮: One（已保存）", LauncherEntryStatus.Deleted("One"));
    }
}
