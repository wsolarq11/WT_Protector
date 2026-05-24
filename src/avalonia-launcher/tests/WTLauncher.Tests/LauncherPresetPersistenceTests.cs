using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetPersistenceTests
{
    [Fact]
    public void LoadReturnsEmptyFileAndMessageForInvalidJson()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        File.WriteAllText(presetPath, "{ not-json");

        var error = LauncherPresetPersistence.Load(presetPath, out var presetFile);

        Assert.NotNull(error);
        Assert.Equal(1, presetFile.SchemaVersion);
        Assert.Equal(string.Empty, presetFile.DefaultPresetId);
        Assert.Empty(presetFile.Presets);
    }

    [Fact]
    public void SaveReturnsMessageWhenTargetPathIsInvalid()
    {
        using var temp = new TempDirectory();
        var presetPath = temp.Path;
        var presetFile = new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
            ],
        };

        var saved = LauncherPresetPersistence.Save(presetFile, presetPath, out var errorMessage);

        Assert.False(saved);
        Assert.NotNull(errorMessage);
    }
}
