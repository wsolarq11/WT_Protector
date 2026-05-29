using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherPresetStatusTests
{
    [Fact]
    public void StatusTextUsesPresetNameAndEntryCounts()
    {
        Assert.Equal("已应用启动组: Daily（2 项，已保存）", LauncherPresetStatus.Applied("Daily", 2));
        Assert.Equal("已保存启动组: Daily（2 项）", LauncherPresetStatus.Saved("Daily", 2));
        Assert.Equal("已更新启动组: Daily（2 项）", LauncherPresetStatus.Updated("Daily", 2));
        Assert.Equal("已删除启动组: Daily", LauncherPresetStatus.Deleted("Daily"));
    }

    [Fact]
    public void StatusTextDescribesPresetFailuresAndCanceledActions()
    {
        Assert.Equal("未选择启动组", LauncherPresetStatus.NoPresetSelected);
        Assert.Equal("已取消: 保存启动组", LauncherPresetStatus.SaveCanceled);
        Assert.Equal("启动组名称不能为空", LauncherPresetStatus.NameRequired);
        Assert.Equal("当前启动组已不存在，请重新加载", LauncherPresetStatus.CurrentPresetMissing);
        Assert.Equal("系统启动组不能更新: Daily", LauncherPresetStatus.UpdateBlocked("Daily"));
        Assert.Equal("启动组不能删除: Daily", LauncherPresetStatus.DeleteBlocked("Daily"));
        Assert.Equal("已取消: 删除启动组 Daily", LauncherPresetStatus.DeleteCanceled("Daily"));
        Assert.Equal(
            "启动组引用了不存在的启动项: one, two",
            LauncherPresetStatus.MissingEntryReferences(["one", "two"]));
    }
}
