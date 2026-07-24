using System.Text.Json.Serialization;
using System.Collections.Immutable;

namespace WTLauncher.Core.Events;

/// <summary>
/// Base type for all state-changing events in the launcher.
/// Every mutation is captured as an immutable event record — the event log
/// is the single source of truth. Projections (LauncherStoreFile) are
/// derived by replaying all events through <see cref="LauncherStoreProjector"/>.
///
/// When adding a new event type:
///   1. Create a sealed record inheriting from LauncherEvent
///   2. Add the projection case in LauncherStoreProjector.Project
///   3. Add the event to EventJournal known types if using JSON serialization
/// </summary>
public abstract record LauncherEvent
{
    /// <summary>Unique event ID (monotonically increasing).</summary>
    [JsonRequired]
    public long Id { get; init; }

    /// <summary>ISO 8601 UTC timestamp of when the event occurred.</summary>
    [JsonRequired]
    public string TimestampUtc { get; init; } = "";

    protected LauncherEvent() { }
}

// ── Entry events ──────────────────────────────────────────────────

public sealed record EntryAddedEvent : LauncherEvent
{
    [JsonRequired]
    public LaunchEntry Entry { get; init; } = null!;
}

public sealed record EntryUpdatedEvent : LauncherEvent
{
    [JsonRequired]
    public string EntryId { get; init; } = "";
    [JsonRequired]
    public LaunchEntry Entry { get; init; } = null!;
}

public sealed record EntryRemovedEvent : LauncherEvent
{
    [JsonRequired]
    public string EntryId { get; init; } = "";
}

public sealed record EntrySelectionChangedEvent : LauncherEvent
{
    [JsonRequired]
    public string EntryId { get; init; } = "";
    [JsonRequired]
    public bool Selected { get; init; }
}

// ── Order events ──────────────────────────────────────────────────

public sealed record EntryOrderChangedEvent : LauncherEvent
{
    [JsonRequired]
    public ImmutableList<string> CustomEntryOrder { get; init; } = ImmutableList<string>.Empty;
}

// ── Preset events ─────────────────────────────────────────────────

public sealed record PresetAddedEvent : LauncherEvent
{
    [JsonRequired]
    public LauncherPreset Preset { get; init; } = null!;
}

public sealed record PresetUpdatedEvent : LauncherEvent
{
    [JsonRequired]
    public LauncherPreset Preset { get; init; } = null!;
}

public sealed record PresetRemovedEvent : LauncherEvent
{
    [JsonRequired]
    public string PresetId { get; init; } = "";
}

public sealed record DefaultPresetChangedEvent : LauncherEvent
{
    [JsonRequired]
    public string DefaultPresetId { get; init; } = "";
}

// ── Settings events ───────────────────────────────────────────────

public sealed record WindowGeometryChangedEvent : LauncherEvent
{
    [JsonRequired]
    public double Width { get; init; }
    [JsonRequired]
    public double Height { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public string WindowState { get; init; } = "Normal";
}

public sealed record SortModeChangedEvent : LauncherEvent
{
    [JsonRequired]
    public string SortMode { get; init; } = "";
}

public sealed record ViewModeChangedEvent : LauncherEvent
{
    [JsonRequired]
    public string ViewMode { get; init; } = "";
}

public sealed record SettingsUpdatedEvent : LauncherEvent
{
    [JsonRequired]
    public LauncherSettings Settings { get; init; } = null!;
}
