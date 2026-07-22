using System.Text.Json.Serialization;

namespace WTLauncher.Core.EventStore;

/// <summary>
/// Source-generated JSON serialization context for EventStore types.
/// Required for Native AOT compatibility — the reflection-based overloads
/// (Serialize(object, Type, ...)) trigger IL2026/IL3050 in trimmed builds.
/// </summary>
[JsonSerializable(typeof(LauncherEvent))]
[JsonSerializable(typeof(EntryAddedEvent))]
[JsonSerializable(typeof(EntryRemovedEvent))]
[JsonSerializable(typeof(EntryUpdatedEvent))]
[JsonSerializable(typeof(SettingsUpdatedEvent))]
[JsonSerializable(typeof(PresetAddedEvent))]
[JsonSerializable(typeof(PresetRemovedEvent))]
internal sealed partial class EventStoreJsonContext : JsonSerializerContext
{
}
