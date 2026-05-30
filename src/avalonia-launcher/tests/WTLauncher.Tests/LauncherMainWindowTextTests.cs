using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherMainWindowTextTests
{
    [Fact]
    public void TextDescribesInitialMainWindowState()
    {
        Assert.Equal("Ready", LauncherMainWindowText.Ready);
        Assert.Equal("选择一个启动项查看预演命令。", LauncherMainWindowText.DryRunPlaceholder);
        Assert.Equal("选择启动组后可查看与当前勾选的差异。", LauncherMainWindowText.PresetSelectionPlaceholder);
    }

    [Fact]
    public void TextDescribesConfigPathsAndLoadedCounts()
    {
        Assert.Equal(
            "配置: D:\\config\\config.json · 启动组: D:\\config\\presets.json",
            LauncherMainWindowText.ConfigPathDisplay("D:\\config\\config.json", "D:\\config\\presets.json"));
        Assert.Equal(
            "已加载 2 个启动项、3 个启动组；勾选自动保存到 config\\config.json",
            LauncherMainWindowText.Loaded(entryCount: 2, presetCount: 3));
    }
}
