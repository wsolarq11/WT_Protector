using System.Collections.Immutable;
using System.Text.Json;
using WTLauncher.Core;
using WTLauncher.Core.Messaging;

namespace WTLauncher.Tests;

public sealed class FeatureMessagesTests
{
    [Fact]
    public void EntryAddedMessage_HoldsEntry()
    {
        var entry = new LaunchEntry { Id = "e1", Name = "Test", Directory = "D:\\", Command = "pwsh" };
        var msg = new EntryAddedMessage(entry);
        Assert.Same(entry, msg.Entry);
    }

    [Fact]
    public void EntryDeletedMessage_HoldsEntryId()
    {
        var msg = new EntryDeletedMessage("e1");
        Assert.Equal("e1", msg.EntryId);
    }

    [Fact]
    public void EntryUpdatedMessage_HoldsOldAndNewIds()
    {
        var entry = new LaunchEntry { Id = "e2", Name = "Renamed", Directory = "D:\\", Command = "pwsh" };
        var msg = new EntryUpdatedMessage("e1", "e2", entry);
        Assert.Equal("e1", msg.OldId);
        Assert.Equal("e2", msg.NewId);
        Assert.Same(entry, msg.Entry);
    }

    [Fact]
    public void SelectionChangedMessage_HoldsSelectedIds()
    {
        var ids = ImmutableList.Create("e1", "e2");
        var msg = new SelectionChangedMessage(ids);
        Assert.Equal(2, msg.SelectedIds.Count);
        Assert.Contains("e1", msg.SelectedIds);
        Assert.Contains("e2", msg.SelectedIds);
    }

    [Fact]
    public void EntryOrderChangedMessage_HoldsOrderedIds()
    {
        var ids = ImmutableList.Create("e2", "e1");
        var msg = new EntryOrderChangedMessage(ids);
        Assert.Equal(2, msg.OrderedIds.Count);
        Assert.Equal("e2", msg.OrderedIds[0]);
        Assert.Equal("e1", msg.OrderedIds[1]);
    }

    [Fact]
    public void SettingsChangedMessage_HoldsSettings()
    {
        var settings = new LauncherSettings { SortMode = "alphabetical" };
        var msg = new SettingsChangedMessage(settings);
        Assert.Equal("alphabetical", msg.Settings.SortMode);
    }

    [Fact]
    public void Messages_AreReferenceType_CompatibleWithWeakReferenceMessenger()
    {
        // WeakReferenceMessenger requires message types to be classes (reference types).
        // Sealed records are classes, so this test verifies the constraint.
        Assert.True(typeof(EntryAddedMessage).IsClass);
        Assert.True(typeof(EntryDeletedMessage).IsClass);
        Assert.True(typeof(EntryUpdatedMessage).IsClass);
        Assert.True(typeof(SelectionChangedMessage).IsClass);
        Assert.True(typeof(EntryOrderChangedMessage).IsClass);
        Assert.True(typeof(SettingsChangedMessage).IsClass);
    }

    /// <summary>
    /// The JSON-serialized message payload uses System.Text.Json source-gen
    /// similar to LauncherJson — verify round-trip for data that crosses
    /// message boundaries.
    /// </summary>
    [Fact]
    public void EntryAddedMessage_Entry_JsonRoundTrips()
    {
        var entry = new LaunchEntry
        {
            Id = "e1",
            Name = "测试",
            Directory = "D:\\test",
            Command = "pwsh",
            Selected = true,
            UpdatedAtUtc = "2026-07-19T00:00:00",
            LaunchedAtUtc = "2026-07-18T00:00:00",
            LaunchCount = 5,
        };
        var msg = new EntryAddedMessage(entry);

        var json = JsonSerializer.Serialize(msg.Entry, LauncherJsonContext.Default.LaunchEntry);
        var deserialized = JsonSerializer.Deserialize(json, LauncherJsonContext.Default.LaunchEntry);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.Id, deserialized.Id);
        Assert.Equal(entry.Name, deserialized.Name);
        Assert.Equal(entry.Directory, deserialized.Directory);
    }
}
