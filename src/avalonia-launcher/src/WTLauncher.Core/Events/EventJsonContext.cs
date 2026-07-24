using System.Text.Json.Serialization;

namespace WTLauncher.Core.Events;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EntryAddedEvent))]
[JsonSerializable(typeof(EntryUpdatedEvent))]
[JsonSerializable(typeof(EntryRemovedEvent))]
[JsonSerializable(typeof(EntrySelectionChangedEvent))]
[JsonSerializable(typeof(EntryOrderChangedEvent))]
[JsonSerializable(typeof(PresetAddedEvent))]
[JsonSerializable(typeof(PresetUpdatedEvent))]
[JsonSerializable(typeof(PresetRemovedEvent))]
[JsonSerializable(typeof(DefaultPresetChangedEvent))]
[JsonSerializable(typeof(WindowGeometryChangedEvent))]
[JsonSerializable(typeof(SortModeChangedEvent))]
[JsonSerializable(typeof(ViewModeChangedEvent))]
[JsonSerializable(typeof(SettingsUpdatedEvent))]
internal sealed partial class EventJsonContext : JsonSerializerContext
{
}
