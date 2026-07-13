using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherStatusPresentationTests
{
    [Theory]
    [InlineData("已新增按钮: 演示（已保存）", LauncherStatusTone.Success)]
    [InlineData("启动成功: 演示", LauncherStatusTone.Success)]
    [InlineData("已取消新增", LauncherStatusTone.Warning)]
    [InlineData("已添加 2 个（1 个跳过）", LauncherStatusTone.Warning)]
    [InlineData("存储保存失败: access denied", LauncherStatusTone.Error)]
    [InlineData("命令模板不存在", LauncherStatusTone.Error)]
    [InlineData("已加载 20 个按钮", LauncherStatusTone.Neutral)]
    [InlineData("正在扫描", LauncherStatusTone.Neutral)]
    public void Classify_MapsUserFacingStatus(string text, LauncherStatusTone expected)
    {
        Assert.Equal(expected, LauncherStatusPresentation.Classify(text));
    }

    [Theory]
    [InlineData(LauncherStatusTone.Neutral, "•")]
    [InlineData(LauncherStatusTone.Success, "✓")]
    [InlineData(LauncherStatusTone.Warning, "!")]
    [InlineData(LauncherStatusTone.Error, "×")]
    public void IndicatorFor_ProvidesNonColorCue(LauncherStatusTone tone, string expected)
    {
        Assert.Equal(expected, LauncherStatusPresentation.IndicatorFor(tone));
    }
}
