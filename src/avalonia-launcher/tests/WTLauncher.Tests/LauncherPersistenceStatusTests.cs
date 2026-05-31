using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherPersistenceStatusTests
{
    [Fact]
    public void StatusTextDescribesPersistenceFailures()
    {
        var exception = new InvalidOperationException("disk blocked");

        Assert.Equal("配置保存失败: disk blocked", LauncherPersistenceStatus.ConfigSaveFailed(exception));
        Assert.Equal("启动组保存失败: disk blocked", LauncherPersistenceStatus.PresetSaveFailed(exception));
        Assert.Equal("设置保存失败: disk blocked", LauncherPersistenceStatus.SettingsSaveFailed(exception));
    }

    [Fact]
    public void RollbackFailedAppendsRestoreErrorsOnlyWhenPresent()
    {
        Assert.Equal("设置保存失败: disk blocked", LauncherPersistenceStatus.RollbackFailed(
            "设置保存失败: disk blocked",
            []));
        Assert.Equal(
            "设置保存失败: disk blocked；回滚磁盘状态失败: config: denied；presets: denied",
            LauncherPersistenceStatus.RollbackFailed(
                "设置保存失败: disk blocked",
                ["config: denied", "presets: denied"]));
    }
}
