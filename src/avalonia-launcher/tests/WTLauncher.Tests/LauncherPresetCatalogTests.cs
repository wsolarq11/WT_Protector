using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetCatalogTests
{
    [Fact]
    public void ApplyMapsPresetFileToViewModelsAndPrefersRequestedPreset()
    {
        var presetFile = new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "team", Name = "Team", EntryIds = ["two"] },
            ],
        };
        var presets = new ObservableCollection<LauncherPresetViewModel>
        {
            new(new LauncherPreset { Id = "stale", Name = "Stale" }),
        };

        var selected = LauncherPresetCatalog.Apply(presetFile, presets, preferredPresetId: "team");

        Assert.Equal(["daily", "team"], presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("team", selected?.Id);
    }

    [Fact]
    public void ApplyFallsBackToDefaultThenFirstPreset()
    {
        var presetFile = new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "team", Name = "Team", EntryIds = ["two"] },
            ],
        };

        var defaultSelected = LauncherPresetCatalog.Apply(presetFile, [], preferredPresetId: null);
        presetFile.DefaultPresetId = "missing";
        var firstSelected = LauncherPresetCatalog.Apply(presetFile, [], preferredPresetId: "also-missing");

        Assert.Equal("daily", defaultSelected?.Id);
        Assert.Equal("daily", firstSelected?.Id);
    }

    [Fact]
    public void BuildSelectionPreviewHandlesNoPresetAndSelectedPreset()
    {
        var noPreset = LauncherPresetCatalog.BuildSelectionPreview(
            selectedPreset: null,
            presetCount: 2,
            selectedEntryIds: ["one"],
            allEntryIds: ["one", "two"]);
        var selected = LauncherPresetCatalog.BuildSelectionPreview(
            new LauncherPresetViewModel(new LauncherPreset
            {
                Id = "daily",
                Name = "Daily",
                EntryIds = ["one", "two"],
            }),
            presetCount: 1,
            selectedEntryIds: ["one"],
            allEntryIds: ["one", "two"]);

        Assert.Equal("应用启动组", noPreset.ApplyButtonText);
        Assert.Contains("缺 1", selected.StatusText);
    }
}
