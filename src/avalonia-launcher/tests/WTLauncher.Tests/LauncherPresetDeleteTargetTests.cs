using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetDeleteTargetTests
{
    [Fact]
    public void TargetSnapshotsNormalizedPresetFactsAndEntryCount()
    {
        var preset = new LauncherPreset
        {
            Id = " Daily Group ",
            Name = " Daily ",
            EntryIds = [" one ", " two "],
        };

        var target = new LauncherPresetDeleteTarget(preset.Id, preset.Name, preset.EntryIds);
        preset = preset with { Id = "changed", Name = "Changed" };
        preset.EntryIds.Clear();

        Assert.Equal("daily-group", target.PresetId);
        Assert.Equal("Daily", target.PresetName);
        Assert.Equal(2, target.EntryCount);
    }

    [Fact]
    public void TargetSnapshotsExplicitPresetFactsAndEntryCount()
    {
        var entryIds = new List<string> { " one ", " two " };

        var target = new LauncherPresetDeleteTarget(" Daily Group ", " Daily ", entryIds);
        entryIds.Clear();

        Assert.Equal("daily-group", target.PresetId);
        Assert.Equal("Daily", target.PresetName);
        Assert.Equal(2, target.EntryCount);
    }

    [Theory]
    [InlineData(" ", "Daily", "Launcher preset delete target id is required.")]
    [InlineData("daily", " ", "Preset name is required.")]
    public void TargetRejectsBlankPresetFacts(string id, string name, string expected)
    {
        var preset = new LauncherPreset
        {
            Id = id,
            Name = name,
            EntryIds = ["one"],
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new LauncherPresetDeleteTarget(preset.Id, preset.Name, preset.EntryIds));

        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public void TargetRejectsInvalidEntryReferences()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", " "],
        };

        var exception = Assert.Throws<ArgumentException>(() => new LauncherPresetDeleteTarget(preset.Id, preset.Name, preset.EntryIds));

        Assert.Equal("entryIds", exception.ParamName);
        Assert.Contains("Entry id 1 is required.", exception.Message);
    }
}
