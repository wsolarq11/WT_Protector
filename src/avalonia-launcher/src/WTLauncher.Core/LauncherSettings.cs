using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable runtime settings. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>. Collection properties use
/// ImmutableList/ImmutableDictionary for true immutability.</summary>
public sealed record LauncherSettings
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

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

    [JsonPropertyName("commandTemplates")]
    public ImmutableDictionary<string, string> CommandTemplates { get; init; } = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("searchHistory")]
    public ImmutableList<string> SearchHistory { get; init; } = ImmutableList<string>.Empty;

    [JsonPropertyName("window")]
    [JsonRequired]
    public LauncherWindowSettings Window { get; init; } = new();
}

/// <summary>Immutable window bounds. All properties are init-only.</summary>
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
}
