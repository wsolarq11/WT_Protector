using System.Text.Json.Serialization;
using WTLauncher.Core;

namespace WTLauncher.Core.EventStore;

[JsonDerivedType(typeof(EntryAddedEvent), typeDiscriminator: "entryAdded")]
[JsonDerivedType(typeof(EntryRemovedEvent), typeDiscriminator: "entryRemoved")]
[JsonDerivedType(typeof(EntryUpdatedEvent), typeDiscriminator: "entryUpdated")]
[JsonDerivedType(typeof(SettingsUpdatedEvent), typeDiscriminator: "settingsUpdated")]
[JsonDerivedType(typeof(PresetAddedEvent), typeDiscriminator: "presetAdded")]
[JsonDerivedType(typeof(PresetRemovedEvent), typeDiscriminator: "presetRemoved")]
public abstract record LauncherEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
}

public sealed record EntryAddedEvent(LaunchEntry Entry) : LauncherEvent;

public sealed record EntryRemovedEvent(string EntryId) : LauncherEvent;

public sealed record EntryUpdatedEvent(LaunchEntry OldEntry, LaunchEntry NewEntry) : LauncherEvent;

public sealed record SettingsUpdatedEvent(LauncherSettings OldSettings, LauncherSettings NewSettings) : LauncherEvent;

public sealed record PresetAddedEvent(LauncherPreset Preset) : LauncherEvent;

public sealed record PresetRemovedEvent(string PresetId) : LauncherEvent;
