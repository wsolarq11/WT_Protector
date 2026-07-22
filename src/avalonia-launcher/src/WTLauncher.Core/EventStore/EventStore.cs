using System.Text.Json;

namespace WTLauncher.Core.EventStore;

public sealed class EventStore
{
    private readonly string _eventLogPath;
    private readonly object _lock = new();

    public EventStore(string configDirectory, JsonSerializerOptions? jsonOptions = null)
    {
        _eventLogPath = Path.Combine(configDirectory, "events.jsonl");
        Directory.CreateDirectory(configDirectory);
    }

    public void Append(LauncherEvent @event)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(@event, EventStoreJsonContext.Default.LauncherEvent);
            File.AppendAllText(_eventLogPath, json + Environment.NewLine);
        }
    }

    /// <summary>
    /// Rebuilds the store file by replaying all events from the JSONL log
    /// on top of the initial snapshot. This is a simplified version —
    /// in production, implement incremental snapshots for large logs.
    /// </summary>
    public LauncherStoreFile RebuildStore(LauncherStoreFile initial)
    {
        lock (_lock)
        {
            if (!File.Exists(_eventLogPath))
                return initial;

            var store = initial;
            var lines = File.ReadAllLines(_eventLogPath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var replayed = JsonSerializer.Deserialize(line, EventStoreJsonContext.Default.LauncherEvent);
                    if (replayed is not null)
                        store = Apply(store, replayed);
                }
                catch (JsonException)
                {
                    // ponytail: skip corrupt event lines instead of failing the entire rebuild.
                    // In production, log the skipped line for manual inspection.
                }
            }

            return store;
        }
    }

    private static LauncherStoreFile Apply(LauncherStoreFile store, LauncherEvent @event)
    {
        return @event switch
        {
            EntryAddedEvent e => store with { Entries = store.Entries.Add(e.Entry) },
            EntryRemovedEvent e => store with { Entries = store.Entries.RemoveAll(x => x.Id.Value == e.EntryId) },
            EntryUpdatedEvent e => store with { Entries = store.Entries.Replace(e.OldEntry, e.NewEntry) },
            SettingsUpdatedEvent e => store with { Settings = e.NewSettings },
            PresetAddedEvent e => store with { Presets = store.Presets.Add(e.Preset) },
            PresetRemovedEvent e => store with { Presets = store.Presets.RemoveAll(x => x.Id == e.PresetId) },
            _ => store
        };
    }
}
