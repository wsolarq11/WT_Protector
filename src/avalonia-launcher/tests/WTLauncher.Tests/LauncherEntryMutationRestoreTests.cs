using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationRestoreTests
{
    [Fact]
    public void RestoreReplacesEntriesSettingsAndPresetFileFromSnapshot()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = new ObservableCollection<LauncherEntryViewModel>
        {
            new(Entry("stale", selected: false), owner),
        };
        var settings = new LauncherSettings { CustomEntryOrder = ["stale"] };
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry("one", selected: true), Entry("two", selected: false)],
            Presets("daily", "one"),
            customEntryOrder: ["two", "one"],
            selectedPresetId: "daily");
        LauncherPresetFile? restoredPresetFile = null;
        string? restoredPresetId = null;

        var summary = LauncherEntryMutationRestore.Restore(
            snapshot,
            entries,
            settings,
            (presetFile, preferredPresetId) =>
            {
                restoredPresetFile = presetFile;
                restoredPresetId = preferredPresetId;
            },
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(1, summary.SelectedCount);
        Assert.Equal(["one", "two"], entries.Select(entry => entry.Id).ToArray());
        Assert.True(entries.Single(entry => entry.Id == "one").IsSelected);
        Assert.Equal(["two", "one"], settings.CustomEntryOrder);
        Assert.Equal("daily", restoredPresetId);
        Assert.NotNull(restoredPresetFile);
        var preset = Assert.Single(restoredPresetFile!.Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal(["one"], preset.EntryIds);
    }

    private static MainWindowViewModel CreateOwner(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "config.json");
        var settingsPath = Path.Combine(rootPath, "settings.json");
        LauncherConfigStore.Save([Entry("owner", selected: false)], configPath);
        return new MainWindowViewModel(configPath, settingsPath, new LauncherSettings(), new LauncherProcess());
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
            Selected = selected,
        };
    }

    private static LauncherPresetFile Presets(string presetId, string entryId)
    {
        return new LauncherPresetFile
        {
            DefaultPresetId = presetId,
            Presets =
            [
                new LauncherPreset
                {
                    Id = presetId,
                    Name = presetId,
                    EntryIds = [entryId],
                },
            ],
        };
    }
}
