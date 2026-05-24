using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesMutableEntryPresetAndOrderState()
    {
        var entry = new LaunchEntry
        {
            Id = "one",
            Name = "One",
            Directory = "D:\\one",
            Command = "pwsh",
            Selected = true,
        };
        var presetFile = new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ["one"],
                },
            ],
        };
        var order = new List<string> { "one" };

        var snapshot = LauncherEntryMutationSnapshot.Capture([entry], presetFile, order, selectedPresetId: "daily");
        entry.Id = "changed";
        presetFile.Presets[0].EntryIds[0] = "changed";
        order[0] = "changed";

        Assert.Equal("daily", snapshot.SelectedPresetId);
        Assert.Equal("one", Assert.Single(snapshot.Entries).Id);
        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(snapshot.CustomEntryOrder));
    }

    [Fact]
    public void CloneMethodsReturnFreshMutableCopies()
    {
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [new LaunchEntry { Id = "one", Name = "One", Directory = "D:\\one", Command = "pwsh" }],
            new LauncherPresetFile
            {
                DefaultPresetId = "daily",
                Presets =
                [
                    new LauncherPreset
                    {
                        Id = "daily",
                        Name = "Daily",
                        EntryIds = ["one"],
                    },
                ],
            },
            ["one"],
            selectedPresetId: "daily");

        var entries = snapshot.CloneEntries();
        var presets = snapshot.ClonePresetFile();
        var order = snapshot.CloneCustomEntryOrder();
        entries[0].Id = "changed";
        presets.Presets[0].EntryIds[0] = "changed";
        order[0] = "changed";

        Assert.Equal("one", Assert.Single(snapshot.Entries).Id);
        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(snapshot.CustomEntryOrder));
    }
}
