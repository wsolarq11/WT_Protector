using System.Text;
using System.Text.Json;
using System.Collections.Immutable;

namespace WTLauncher.Core.Events;

/// <summary>
/// Append-only event journal with AOT-safe serialization.
/// All writes use FileOptions.WriteThrough for crash safety.
/// </summary>
public static class EventJournal
{
    private const long MaxSegmentSize = 10 * 1024 * 1024;
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private static long _eventSequence;

    /// <summary>Append an event to the journal. Returns the assigned event ID.</summary>
    public static async Task<long> Append(string journalDir, LauncherEvent @event, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalDir);
        ArgumentNullException.ThrowIfNull(@event);

        await WriteLock.WaitAsync(ct);
        try
        {
            EnsureDirectory(journalDir);
            var id = Interlocked.Increment(ref _eventSequence);
            var stamped = @event with { Id = id, TimestampUtc = DateTimeOffset.UtcNow.ToString("O") };

            var json = SerializeEvent(stamped);
            if (json == null)
                throw new InvalidOperationException($"Cannot serialize event type: {stamped.GetType().Name}");

            var logPath = Path.Combine(journalDir, "events.log");
            await using var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write,
                FileShare.Read, 4096, FileOptions.WriteThrough | FileOptions.Asynchronous);
            await fs.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), ct);
            await fs.FlushAsync(ct);

            if (fs.Length > MaxSegmentSize)
                RollSegment(journalDir);

            return id;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <summary>Read all events from the journal.</summary>
    public static async Task<ImmutableList<LauncherEvent>> ReadAll(string journalDir)
    {
        var logPath = Path.Combine(journalDir, "events.log");
        if (!File.Exists(logPath))
            return [];

        var events = new List<LauncherEvent>();
        var lines = await File.ReadAllLinesAsync(logPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var ev = DeserializeEvent(line);
                if (ev != null) events.Add(ev);
            }
            catch (JsonException) { /* skip corrupt line */ }
        }
        return [.. events];
    }

    // ── AOT-safe dispatch ─────────────────────────────────────

    private static string? SerializeEvent(LauncherEvent @event) => @event switch
    {
        EntryAddedEvent e           => JsonSerializer.Serialize(e, EventJsonContext.Default.EntryAddedEvent),
        EntryUpdatedEvent e        => JsonSerializer.Serialize(e, EventJsonContext.Default.EntryUpdatedEvent),
        EntryRemovedEvent e        => JsonSerializer.Serialize(e, EventJsonContext.Default.EntryRemovedEvent),
        EntrySelectionChangedEvent e => JsonSerializer.Serialize(e, EventJsonContext.Default.EntrySelectionChangedEvent),
        EntryOrderChangedEvent e   => JsonSerializer.Serialize(e, EventJsonContext.Default.EntryOrderChangedEvent),
        PresetAddedEvent e         => JsonSerializer.Serialize(e, EventJsonContext.Default.PresetAddedEvent),
        PresetUpdatedEvent e       => JsonSerializer.Serialize(e, EventJsonContext.Default.PresetUpdatedEvent),
        PresetRemovedEvent e       => JsonSerializer.Serialize(e, EventJsonContext.Default.PresetRemovedEvent),
        DefaultPresetChangedEvent e => JsonSerializer.Serialize(e, EventJsonContext.Default.DefaultPresetChangedEvent),
        WindowGeometryChangedEvent e => JsonSerializer.Serialize(e, EventJsonContext.Default.WindowGeometryChangedEvent),
        SortModeChangedEvent e     => JsonSerializer.Serialize(e, EventJsonContext.Default.SortModeChangedEvent),
        ViewModeChangedEvent e     => JsonSerializer.Serialize(e, EventJsonContext.Default.ViewModeChangedEvent),
        SettingsUpdatedEvent e     => JsonSerializer.Serialize(e, EventJsonContext.Default.SettingsUpdatedEvent),
        _ => null
    };

    private static LauncherEvent? DeserializeEvent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Type discrimination via property presence
        if (root.TryGetProperty("entry", out _) && !root.TryGetProperty("entryId", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.EntryAddedEvent);
        if (root.TryGetProperty("entryId", out _) && root.TryGetProperty("entry", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.EntryUpdatedEvent);
        if (root.TryGetProperty("entryId", out _) && root.TryGetProperty("selected", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.EntrySelectionChangedEvent);
        if (root.TryGetProperty("entryId", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.EntryRemovedEvent);
        if (root.TryGetProperty("customEntryOrder", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.EntryOrderChangedEvent);
        if (root.TryGetProperty("preset", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.PresetAddedEvent);
        if (root.TryGetProperty("presetId", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.PresetRemovedEvent);
        if (root.TryGetProperty("defaultPresetId", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.DefaultPresetChangedEvent);
        if (root.TryGetProperty("width", out _) && root.TryGetProperty("height", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.WindowGeometryChangedEvent);
        if (root.TryGetProperty("sortMode", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.SortModeChangedEvent);
        if (root.TryGetProperty("viewMode", out _))
            return JsonSerializer.Deserialize(json, EventJsonContext.Default.ViewModeChangedEvent);

        return null;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private static void RollSegment(string journalDir)
    {
        var logPath = Path.Combine(journalDir, "events.log");
        var backupPath = Path.Combine(journalDir, "events.1.log");
        try
        {
            if (File.Exists(backupPath)) File.Delete(backupPath);
            File.Move(logPath, backupPath);
        }
        catch (IOException) { /* best-effort */ }
    }
}
