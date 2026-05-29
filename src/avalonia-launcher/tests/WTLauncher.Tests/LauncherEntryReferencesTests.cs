using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryReferencesTests
{
    [Fact]
    public void RenameEntryIdUpdatesPresetsAndCustomOrder()
    {
        var presetFile = PresetFile(
            Preset("daily", "one", "two", "one"),
            Preset("manual", "three"));
        var customEntryOrder = new List<string> { "one", "two", "one", "three" };

        LauncherEntryReferences.RenameEntryId(
            presetFile,
            customEntryOrder,
            oldEntryId: "one",
            newEntryId: "one-renamed");

        Assert.Equal(["one-renamed", "two", "one-renamed"], presetFile.Presets[0].EntryIds);
        Assert.Equal(["three"], presetFile.Presets[1].EntryIds);
        Assert.Equal(["one-renamed", "two", "one-renamed", "three"], customEntryOrder);
    }

    [Fact]
    public void RemoveEntryIdRemovesPresetsAndCustomOrderReferences()
    {
        var presetFile = PresetFile(
            Preset("daily", "one", "two", "one"),
            Preset("manual", "three"));
        var customEntryOrder = new List<string> { "one", "two", "one", "three" };

        LauncherEntryReferences.RemoveEntryId(
            presetFile,
            customEntryOrder,
            entryId: "one");

        Assert.Equal(["two"], presetFile.Presets[0].EntryIds);
        Assert.Equal(["three"], presetFile.Presets[1].EntryIds);
        Assert.Equal(["two", "three"], customEntryOrder);
    }

    private static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            Presets = [.. presets],
        };

    private static LauncherPreset Preset(string id, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = id,
            EntryIds = [.. entryIds],
        };
}
