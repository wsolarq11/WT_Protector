using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherConfigPersistenceTests
{
    [Fact]
    public void SaveWritesEntriesWhenPathIsValid()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var entries = new[]
        {
            new LaunchEntry
            {
                Id = "one",
                Name = "One",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = true,
                Selected = true,
            },
        };

        var saved = LauncherConfigPersistence.Save(entries, configPath, out var errorMessage);

        Assert.True(saved);
        Assert.Null(errorMessage);
        var entry = Assert.Single(LauncherConfigStore.Load(configPath));
        Assert.Equal("one", entry.Id);
        Assert.True(entry.Confirm);
        Assert.True(entry.Selected);
    }

    [Fact]
    public void SaveReturnsMessageWhenTargetPathIsInvalid()
    {
        using var temp = new TempDirectory();

        var saved = LauncherConfigPersistence.Save([], temp.Path, out var errorMessage);

        Assert.False(saved);
        Assert.NotNull(errorMessage);
        Assert.StartsWith("配置保存失败:", errorMessage, StringComparison.Ordinal);
    }
}
