using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsStatusTests
{
    [Fact]
    public void StatusTextDescribesSavedViewSettings()
    {
        Assert.Equal("排序模式: 更新时间（已保存）", LauncherSettingsStatus.SortModeSaved("更新时间"));
        Assert.Equal("视图模式: 卡片（已保存）", LauncherSettingsStatus.DisplayModeSaved("卡片"));
    }

    [Theory]
    [InlineData(true, "启动确认: 所有启动项启动前统一确认")]
    [InlineData(false, "启动确认: 所有启动项直接启动")]
    public void StatusTextDescribesSavedLaunchConfirmation(bool requireConfirmation, string expected)
    {
        Assert.Equal(expected, LauncherSettingsStatus.LaunchConfirmationSaved(requireConfirmation));
    }
}
