using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetStateWorkflowTests
{
    [Fact]
    public void ReloadLoadsAppliesPresetStateAndNotifies()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherPresetStore.Save(CreatePresetFile(), presetPath);
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };
        LauncherPresetState? applied = null;
        var notifyCount = 0;

        var error = LauncherPresetStateWorkflow.Reload(
            presetPath,
            presets,
            preferredPresetId: "team",
            state => applied = state,
            () => notifyCount++);

        Assert.Null(error);
        Assert.Equal(1, notifyCount);
        var appliedState = applied ?? throw new InvalidOperationException("Preset state was not applied.");
        Assert.Equal(2, appliedState.PresetCount);
        Assert.Equal("daily", appliedState.PresetFile.DefaultPresetId);
        Assert.Equal("team", appliedState.SelectedPreset?.Id);
        Assert.Equal(["daily", "team"], presets.Select(preset => preset.Id).ToArray());
    }

    [Fact]
    public void ApplySnapshotRefreshesPresetCollectionAppliesStateAndNotifies()
    {
        var presetFile = CreatePresetFile();
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };
        LauncherPresetState? applied = null;
        var notifyCount = 0;

        LauncherPresetStateWorkflow.ApplySnapshot(
            presetFile,
            presets,
            preferredPresetId: "team",
            state => applied = state,
            () => notifyCount++);

        Assert.Equal(1, notifyCount);
        var appliedState = applied ?? throw new InvalidOperationException("Preset state was not applied.");
        Assert.Same(presetFile, appliedState.PresetFile);
        Assert.Equal(2, appliedState.PresetCount);
        Assert.Equal("team", appliedState.SelectedPreset?.Id);
        Assert.Equal(["daily", "team"], presets.Select(preset => preset.Id).ToArray());
    }

    private static LauncherPresetFile CreatePresetFile()
    {
        return new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "team", Name = "Team", EntryIds = ["two"] },
            ],
        };
    }
}
