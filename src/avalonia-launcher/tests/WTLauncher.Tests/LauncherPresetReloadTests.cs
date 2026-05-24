using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetReloadTests
{
    [Fact]
    public void LoadPopulatesPresetCollectionAndReturnsSelectedPreset()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = "daily",
                Presets =
                [
                    new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                    new LauncherPreset { Id = "team", Name = "Team", EntryIds = ["two"] },
                ],
            },
            presetPath);
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };

        var result = LauncherPresetReload.Load(presetPath, presets, preferredPresetId: "team");

        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.PresetCount);
        Assert.Equal("team", result.SelectedPreset?.Id);
        Assert.Equal(["daily", "team"], presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", result.PresetFile.DefaultPresetId);
    }

    [Fact]
    public void LoadReturnsEmptyPresetFileAndErrorForInvalidJson()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        File.WriteAllText(presetPath, "{ invalid-json");
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };

        var result = LauncherPresetReload.Load(presetPath, presets, preferredPresetId: "stale");

        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, result.PresetCount);
        Assert.Null(result.SelectedPreset);
        Assert.Empty(result.PresetFile.Presets);
        Assert.Empty(presets);
    }
}
