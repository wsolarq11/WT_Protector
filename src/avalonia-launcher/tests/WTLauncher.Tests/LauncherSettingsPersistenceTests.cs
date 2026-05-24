using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsPersistenceTests
{
    [Fact]
    public void SaveWritesSettingsWhenPathIsValid()
    {
        using var temp = new TempDirectory();
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var settings = new LauncherSettings
        {
            Theme = "dark",
            SortMode = "alphabetical",
            EntryViewMode = "names",
            LastFilterText = "codex",
            RequireConfirmationForLaunches = false,
        };

        var saved = LauncherSettingsPersistence.Save(settings, settingsPath, out var errorMessage);

        Assert.True(saved);
        Assert.Null(errorMessage);
        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("alphabetical", loaded.SortMode);
        Assert.Equal("names", loaded.EntryViewMode);
        Assert.Equal("codex", loaded.LastFilterText);
        Assert.False(loaded.RequireConfirmationForLaunches);
    }

    [Fact]
    public void SaveReturnsMessageWhenTargetPathIsInvalid()
    {
        using var temp = new TempDirectory();

        var saved = LauncherSettingsPersistence.Save(new LauncherSettings(), temp.Path, out var errorMessage);

        Assert.False(saved);
        Assert.NotNull(errorMessage);
        Assert.StartsWith("设置保存失败:", errorMessage, StringComparison.Ordinal);
    }
}
