using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryReferencesTests
{
    [Fact]
    public void RenameUpdatesPresetEntryIdsAndCustomOrder()
    {
        var presets = new[]
        {
            new LauncherPreset
            {
                Id = "daily",
                Name = "Daily",
                EntryIds = ["one", "two", "one"],
            },
            new LauncherPreset
            {
                Id = "manual",
                Name = "Manual",
                EntryIds = [],
            },
        };
        var order = new List<string> { "one", "two", "one" };

        LauncherEntryReferences.Rename("one", "renamed", presets, order);

        Assert.Equal(["renamed", "two", "renamed"], presets[0].EntryIds);
        Assert.Empty(presets[1].EntryIds);
        Assert.Equal(["renamed", "two", "renamed"], order);
    }

    [Fact]
    public void RemoveDeletesEntryIdsFromPresetsAndCustomOrder()
    {
        var presets = new[]
        {
            new LauncherPreset
            {
                Id = "daily",
                Name = "Daily",
                EntryIds = ["one", "two", "one"],
            },
            new LauncherPreset
            {
                Id = "team",
                Name = "Team",
                EntryIds = ["two"],
            },
        };
        var order = new List<string> { "one", "two", "one" };

        LauncherEntryReferences.Remove("one", presets, order);

        Assert.Equal(["two"], presets[0].EntryIds);
        Assert.Equal(["two"], presets[1].EntryIds);
        Assert.Equal(["two"], order);
    }
}
