using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesMutableEntryPresetAndOrderState()
    {
        var entry = Entry(selected: true, updatedAtUtc: "2026-05-20T00:00:00Z");
        var presetFile = PresetFile();
        var order = ImmutableList.Create("one");

        var snapshot = LauncherEntryMutationSnapshot.Capture([entry], presetFile, order, selectedPresetId: "daily");
        // Mutate originals after capture — snapshot must remain unchanged.
        entry = entry with { Id = "changed" };

        Assert.Equal("daily", snapshot.SelectedPresetId);
        var snapshotEntry = Assert.Single(snapshot.Entries);
        Assert.NotSame(entry, snapshotEntry);
        Assert.Equal("one", snapshotEntry.Id);
        Assert.Equal("One", snapshotEntry.Name);
        Assert.Equal("D:\\one", snapshotEntry.Directory);
        Assert.Equal("pwsh", snapshotEntry.Command);
        Assert.True(snapshotEntry.Selected);
        Assert.Equal("2026-05-20T00:00:00Z", snapshotEntry.UpdatedAtUtc);
        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(snapshot.CustomEntryOrder));
    }

    [Fact]
    public void CloneMethodsReturnFreshMutableCopies()
    {
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry()],
            PresetFile(),
            ["one"],
            selectedPresetId: "daily");

        var entries = snapshot.CloneEntries();
        var presets = snapshot.ClonePresetFile();
        var order = snapshot.CloneCustomEntryOrder();
        // CloneEntries returns mutable List<LaunchEntry> copies.
        entries[0] = entries[0] with { Id = "changed" };
        // ClonePresetFile and CloneCustomEntryOrder return immutable copies;
        // verify the originals are unchanged.
        Assert.Equal("one", Assert.Single(snapshot.Entries).Id);
        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(snapshot.CustomEntryOrder));
        // Cloned copies carry the original values.
        Assert.Equal("one", Assert.Single(presets.Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(order));
    }

    [Fact]
    public void PublicAccessorsDoNotExposeRollbackStorage()
    {
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry()],
            PresetFile(),
            ["one"],
            selectedPresetId: "daily");

        var entry = Assert.Single(snapshot.Entries);
        // Entries is a read-only projection; the underlying clone is protected.
        entry = entry with { Id = "changed" };
        var order = Assert.IsAssignableFrom<IList<string>>(snapshot.CustomEntryOrder);

        Assert.True(order.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => order.Add("changed"));
        Assert.Equal("one", Assert.Single(snapshot.CloneEntries()).Id);
        Assert.Equal("one", Assert.Single(snapshot.ClonePresetFile().Presets[0].EntryIds));
        Assert.Equal("one", Assert.Single(snapshot.CloneCustomEntryOrder()));
    }

    private static LaunchEntry Entry(
        bool selected = false,
        string updatedAtUtc = "") =>
        new()
        {
            Id = "one",
            Name = "One",
            Directory = "D:\\one",
            Command = "pwsh",
            Selected = selected,
            UpdatedAtUtc = updatedAtUtc,
        };

    private static LauncherPresetFile PresetFile() =>
        new()
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ImmutableList.Create("one"),
                },
            ],
        };
}
