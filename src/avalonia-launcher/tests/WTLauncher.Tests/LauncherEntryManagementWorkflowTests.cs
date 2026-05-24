using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryManagementWorkflowTests
{
    [Fact]
    public void AddEntryReturnsSavedStatusAndUpdatesCustomOrder()
    {
        using var temp = new TempDirectory();
        var settings = new LauncherSettings { CustomEntryOrder = [] };
        var owner = CreateOwner(temp, settings);
        var snapshot = Snapshot(owner, settings);
        bool? savePresets = null;

        var result = LauncherEntryManagementWorkflow.AddEntry(
            new EntryDraft("one", "One", "D:\\one", "codex", true),
            snapshot,
            owner.Entries,
            settings.CustomEntryOrder,
            entry => new LauncherEntryViewModel(entry, owner),
            "2026-05-24T00:00:00Z",
            (presets, _) =>
            {
                savePresets = presets;
                return true;
            },
            _ => throw new InvalidOperationException("Restore should not run."));

        Assert.Equal("已新增按钮: One（已保存）", result.StatusText);
        Assert.False(savePresets);
        var entry = Assert.Single(owner.Entries);
        Assert.Equal("one", entry.Id);
        Assert.True(entry.IsSelected);
        Assert.Equal(["one"], settings.CustomEntryOrder);
    }

    [Fact]
    public void EditEntryRestoresSnapshotWhenSaveFails()
    {
        using var temp = new TempDirectory();
        var settings = new LauncherSettings { CustomEntryOrder = ["one"] };
        var owner = CreateOwner(temp, settings, Entry("one", "One"));
        var snapshot = Snapshot(owner, settings);
        var restoreCalled = false;
        bool? savePresets = null;

        var result = LauncherEntryManagementWorkflow.EditEntry(
            owner.Entries.Single(),
            new EntryDraft("renamed", "Renamed", "D:\\renamed", "pwsh", false),
            snapshot,
            owner.Entries,
            new List<LauncherPreset>(),
            settings.CustomEntryOrder,
            "2026-05-24T00:00:00Z",
            (presets, _) =>
            {
                savePresets = presets;
                return false;
            },
            _ => restoreCalled = true);

        Assert.Null(result.StatusText);
        Assert.True(savePresets);
        Assert.True(restoreCalled);
    }

    [Fact]
    public void DeleteEntryReturnsSavedStatusAndRemovesReferences()
    {
        using var temp = new TempDirectory();
        var settings = new LauncherSettings { CustomEntryOrder = ["one", "two"] };
        var owner = CreateOwner(temp, settings, Entry("one", "One"), Entry("two", "Two"));
        var snapshot = Snapshot(owner, settings);
        var presets = new List<LauncherPreset>
        {
            new() { Id = "daily", Name = "Daily", EntryIds = ["one", "two"] },
        };
        bool? savePresets = null;

        var result = LauncherEntryManagementWorkflow.DeleteEntry(
            owner.Entries.Single(entry => entry.Id == "one"),
            snapshot,
            owner.Entries,
            presets,
            settings.CustomEntryOrder,
            (presetsChanged, _) =>
            {
                savePresets = presetsChanged;
                return true;
            },
            _ => throw new InvalidOperationException("Restore should not run."));

        Assert.Equal("已删除按钮: One（已保存）", result.StatusText);
        Assert.True(savePresets);
        Assert.DoesNotContain(owner.Entries, entry => entry.Id == "one");
        Assert.Equal(["two"], presets.Single().EntryIds);
        Assert.Equal(["two"], settings.CustomEntryOrder);
    }

    private static MainWindowViewModel CreateOwner(
        TempDirectory temp,
        LauncherSettings settings,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var configPath = Path.Combine(configDir, "config.json");
        LauncherConfigStore.Save(entries, configPath);
        return new MainWindowViewModel(
            configPath,
            Path.Combine(configDir, "settings.json"),
            settings,
            new LauncherProcess());
    }

    private static LauncherEntryMutationSnapshot Snapshot(MainWindowViewModel owner, LauncherSettings settings)
    {
        return LauncherEntryMutationSnapshot.Capture(
            owner.Entries.Select(entry => entry.Entry),
            new LauncherPresetFile(),
            settings.CustomEntryOrder,
            selectedPresetId: null);
    }

    private static LaunchEntry Entry(string id, string name)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = name,
            Directory = $"D:\\{id}",
            Command = "codex",
            Confirm = false,
        };
    }
}
