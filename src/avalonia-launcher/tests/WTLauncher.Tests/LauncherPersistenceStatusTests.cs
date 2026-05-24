using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPersistenceStatusTests
{
    [Fact]
    public void SaveConfigReturnsSavedAndWritesEntries()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");

        var result = LauncherPersistenceStatus.SaveConfig(
            [
                new LaunchEntry
                {
                    Id = "one",
                    Name = "One",
                    Directory = "D:\\one",
                    Command = "codex",
                    Selected = true,
                },
            ],
            configPath);

        Assert.True(result.Succeeded);
        Assert.Null(result.StatusText);
        Assert.True(LauncherConfigStore.Load(configPath).Single().Selected);
    }

    [Fact]
    public void SavePresetsReturnsFailureStatus()
    {
        using var temp = new TempDirectory();

        var result = LauncherPersistenceStatus.SavePresets(new LauncherPresetFile(), temp.Path);

        Assert.False(result.Succeeded);
        Assert.Contains("启动组保存失败", result.StatusText);
    }
}
