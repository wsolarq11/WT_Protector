using System.Text.Json.Serialization;

namespace WTLauncher.Core.EventStore;

// ponytail: Dead code - Event Sourcing was evaluated and scoped out as
// "No-Go" during architecture review. Keep as reference.
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
