using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable runtime settings. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>. Collection properties use
/// ImmutableList/ImmutableDictionary for true immutability.
/// ponytail: SchemaVersion lives only at LauncherStoreFile top level — single source of truth.
/// Old launcher.json files with settings.schemaVersion still load (UnmappedMemberHandling=Skip).</summary>
public sealed record LauncherSettings
{
    [JsonPropertyName("lastFilterText")]
    [JsonRequired]
    public string LastFilterText { get; init; } = "";

    [JsonPropertyName("sortMode")]
    [JsonRequired]
    public string SortMode { get; init; } = LauncherEntryVisibility.CustomSortModeId;

    [JsonPropertyName("entryViewMode")]
    [JsonRequired]
    public string EntryViewMode { get; init; } = LauncherEntryViewModes.CardsId;

    [JsonPropertyName("requireConfirmationForLaunches")]
    [JsonRequired]
    public bool RequireConfirmationForLaunches { get; init; } = true;

    [JsonPropertyName("customEntryOrder")]
    [JsonRequired]
    public ImmutableList<string> CustomEntryOrder { get; init; } = ImmutableList<string>.Empty;

    /// <summary>Key-value command templates. Lookup uses <see cref="StringComparer.OrdinalIgnoreCase"/>.</summary>
    [JsonPropertyName("commandTemplates")]
    public ImmutableDictionary<string, string> CommandTemplates { get; init; } = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("searchHistory")]
    public ImmutableList<string> SearchHistory { get; init; } = ImmutableList<string>.Empty;

    [JsonPropertyName("window")]
    public LauncherWindowSettings Window { get; init; } = new();
}

/// <summary>Immutable window bounds and state. All properties are init-only.</summary>
public sealed record LauncherWindowSettings
{
    public const double DefaultWidth = 1180;

    public const double DefaultHeight = 760;

    [JsonPropertyName("width")]
    [JsonRequired]
    public double Width { get; init; } = DefaultWidth;

    [JsonPropertyName("height")]
    [JsonRequired]
    public double Height { get; init; } = DefaultHeight;

    /// <summary>Window X screen coordinate. Default 0 (top-left of primary monitor).</summary>
    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double X { get; init; }

    /// <summary>Window Y screen coordinate. Default 0 (top-left of primary monitor).</summary>
    [JsonPropertyName("y")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Y { get; init; }

    /// <summary>Window state: Normal, Maximized, or Minimized. Serialized only when non-default.</summary>
    [JsonPropertyName("windowState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string WindowState { get; init; } = "Normal";
}
