using System.Collections.Immutable;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherConfigExchangeTests
{
    [Fact]
    public void BuildExportFile_FiltersEntriesAndPresetsAndLocalSettings()
    {
        var source = Store(
            [Entry("one", "One"), Entry("two", "Two")],
            [Preset("both", "Both", "one", "two"), Preset("one-only", "One Only", "one")],
            defaultPresetId: "both");

        var exported = LauncherConfigExchange.BuildExportFile(source, ["one"]);

        var entry = Assert.Single(exported.Entries);
        Assert.Equal("one", entry.Id.Value);
        Assert.Single(exported.Presets);
        Assert.Equal("one-only", exported.Presets[0].Id);
        Assert.Equal("", exported.DefaultPresetId);
        Assert.Empty(exported.Settings.SearchHistory);
        Assert.Empty(exported.Settings.CustomEntryOrder);
    }

    [Fact]
    public void AnalyzeImport_FlagsDuplicateMissingAndInvalidPresetReference()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Path, "missing");
        var current = Store([Entry("one", "One")], []);
        var imported = Store(
            [Entry("one", "One"), Entry("two", "Two", missing)],
            [Preset("bad", "Bad", "one", "ghost")]);

        var preview = LauncherConfigExchange.AnalyzeImport(current, imported);

        Assert.Contains(preview.Rows, row => row.Id == "one" && row.IsDuplicate);
        Assert.Contains(preview.Rows, row => row.Id == "two" && row.IsMissing);
        Assert.Contains(preview.InvalidPresetReferences, id => id == "ghost");
    }

    [Fact]
    public void ApplyImport_AddNewSkipsDuplicatesAndAppliesPathMapping()
    {
        using var temp = new TempDirectory();
        var existing = Directory.CreateDirectory(Path.Combine(temp.Path, "exists")).FullName;
        var current = Store([Entry("one", "One", existing)], [Preset("one", "One", "one")], "one");
        var imported = Store(
            [Entry("one", "One", existing), Entry("two", "Two", Path.Combine(temp.Path, "missing"))],
            [Preset("two", "Two", "two")],
            "two");
        var plan = new LauncherImportPlan
        {
            Mode = LauncherImportMode.AddNew,
            DirectoryOverrides = new Dictionary<string, string> { ["two"] = existing },
        };

        var merged = LauncherConfigExchange.ApplyImport(current, imported, plan);

        Assert.Equal(2, merged.Entries.Count);
        Assert.Equal(existing, merged.Entries.Single(entry => entry.Id.Value == "two").Directory.Value);
        Assert.Equal("one", merged.DefaultPresetId);
    }

    [Fact]
    public void ApplyImport_ReplaceUsesImportedStateAndDropsInvalidPresets()
    {
        using var temp = new TempDirectory();
        var existing = Directory.CreateDirectory(Path.Combine(temp.Path, "exists")).FullName;
        var current = Store([Entry("old", "Old", existing)], [Preset("old", "Old", "old")], "old");
        var imported = Store(
            [Entry("new", "New", Path.Combine(temp.Path, "missing"))],
            [Preset("new", "New", "new"), Preset("bad", "Bad", "ghost")],
            "new");
        var plan = new LauncherImportPlan
        {
            Mode = LauncherImportMode.Replace,
            DirectoryOverrides = new Dictionary<string, string> { ["new"] = existing },
        };

        var merged = LauncherConfigExchange.ApplyImport(current, imported, plan);

        Assert.Single(merged.Entries);
        Assert.Equal("new", merged.Entries[0].Id.Value);
        Assert.Equal(existing, merged.Entries[0].Directory.Value);
        Assert.Single(merged.Presets);
        Assert.Equal("new", merged.DefaultPresetId);
    }

    private static LaunchEntry Entry(string id, string name, string? directory = null) =>
        new()
        {
            Id = id,
            Name = name,
            Directory = directory is null ? DirectoryPath.Empty : (DirectoryPath)directory,
            Command = "pwsh",
        };

    private static LauncherPreset Preset(string id, string name, params string[] entryIds) =>
        new() { Id = id, Name = name, EntryIds = [.. entryIds] };

    private static LauncherStoreFile Store(
        ImmutableList<LaunchEntry> entries,
        ImmutableList<LauncherPreset> presets,
        string defaultPresetId = "") =>
        new()
        {
            SchemaVersion = 2,
            Entries = entries,
            Presets = presets,
            Settings = new LauncherSettings { SearchHistory = ["local"], CustomEntryOrder = ["one"] },
            DefaultPresetId = defaultPresetId,
        };
}
