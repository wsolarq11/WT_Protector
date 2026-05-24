using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetSelectionRulesTests
{
    [Fact]
    public void BuildPreviewReflectsCurrentEntries()
    {
        var entries = new[]
        {
            Entry("one", selected: true),
            Entry("two", selected: false),
        };
        var selectedPreset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", "two"],
        });

        var preview = LauncherPresetSelectionRules.BuildPreview(selectedPreset, presetCount: 1, entries);

        Assert.Equal(1, preview.MatchedCount);
        Assert.Equal(1, preview.MissingCount);
        Assert.Equal(0, preview.ExtraCount);
        Assert.Equal("应用启动组", preview.ApplyButtonText);
    }

    [Fact]
    public void BuildStateReturnsSelectedCountAndPresetStatusTexts()
    {
        var entries = new[]
        {
            Entry("one", selected: true),
            Entry("two", selected: false),
        };
        var selectedPreset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one"],
        });

        var state = LauncherPresetSelectionRules.BuildState(selectedPreset, presetCount: 1, entries);

        Assert.Equal(1, state.SelectedCount);
        Assert.Equal("已匹配", state.ApplyPresetButtonText);
        Assert.Contains("已匹配启动组", state.PresetSelectionStatusText);
    }

    [Fact]
    public void CanApplyRequiresASelectedPresetWithChanges()
    {
        var entries = new[]
        {
            Entry("one", selected: true),
            Entry("two", selected: false),
        };
        var selectedPreset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["two"],
        });

        Assert.False(LauncherPresetSelectionRules.CanApply(null, 1, entries));
        Assert.True(LauncherPresetSelectionRules.CanApply(selectedPreset, 1, entries));
        Assert.False(LauncherPresetSelectionRules.CanApply(selectedPreset, 1, Array.Empty<LauncherEntryViewModel>()));
    }

    [Fact]
    public void CanUpdateRequiresUnlockedPresetAndSelectedEntries()
    {
        var selectedEntries = new[]
        {
            Entry("one", selected: true),
            Entry("two", selected: false),
        };
        var lockedPreset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["two"],
            Locked = true,
        });
        var unlockedPreset = new LauncherPresetViewModel(new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["two"],
        });

        Assert.False(LauncherPresetSelectionRules.CanUpdate(lockedPreset, 1, selectedEntries));
        Assert.True(LauncherPresetSelectionRules.CanUpdate(unlockedPreset, 1, selectedEntries));
        Assert.False(LauncherPresetSelectionRules.CanUpdate(unlockedPreset, 1, new[] { Entry("one", selected: false) }));
    }

    [Fact]
    public void CanDeleteRequiresUnlockedDeletablePreset()
    {
        var presetFile = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "secondary", Name = "Secondary", EntryIds = ["two"] },
                new LauncherPreset { Id = "blank-manual", Name = "Manual", Locked = true },
            ],
        };
        var daily = new LauncherPresetViewModel(presetFile.Presets.Single(preset => preset.Id == "daily"));
        var locked = new LauncherPresetViewModel(presetFile.Presets.Single(preset => preset.Id == "blank-manual"));
        var singleRemaining = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                new LauncherPreset { Id = "blank-manual", Name = "Manual", Locked = true },
            ],
        };

        Assert.False(LauncherPresetSelectionRules.CanDelete(null, presetFile));
        Assert.False(LauncherPresetSelectionRules.CanDelete(locked, presetFile));
        Assert.True(LauncherPresetSelectionRules.CanDelete(daily, presetFile));
        Assert.False(LauncherPresetSelectionRules.CanDelete(daily, singleRemaining));
    }

    private static LauncherEntryViewModel Entry(string id, bool selected)
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        LauncherConfigStore.Save(
            [
                new LaunchEntry
                {
                    Id = "seed",
                    Name = "Seed",
                    Directory = "D:\\seed",
                    Command = "pwsh",
                    Confirm = false,
                },
            ],
            configPath);
        var owner = new MainWindowViewModel(
            configPath: configPath,
            settingsPath: Path.Combine(temp.Path, "settings.json"),
            settings: new LauncherSettings(),
            launcherProcess: new LauncherProcess());

        var vm = new LauncherEntryViewModel(
            new LaunchEntry
            {
                Id = id,
                Name = id,
                Directory = $"D:\\{id}",
                Command = "pwsh",
                Confirm = false,
                Selected = selected,
            },
            owner);
        return vm;
    }
}
