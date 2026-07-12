using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable runtime settings. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>. Collection properties use
/// ImmutableList/immutable-friendly types for true immutability.
/// ponytail: SchemaVersion lives only at LauncherStoreFile top level — single source of truth.
/// launcher.json is validated against the C# record schema at load time
/// (UnmappedMemberHandling=Disallow), so reintroducing settings.schemaVersion
/// fails loudly rather than silently drifting.</summary>
public sealed record LauncherSettings
{
    [JsonPropertyName("lastFilterText")]
    [JsonRequired]
    public string LastFilterText { get; init; } = "";

    [JsonPropertyName("sortMode")]
    [JsonRequired]
    public string SortMode { get; init; } = LauncherEntryQuery.CustomSortModeId;

    [JsonPropertyName("entryViewMode")]
    [JsonRequired]
    public string EntryViewMode { get; init; } = LauncherEntryViewModes.CardsId;

    /// <summary>Cards 视图分组维度: none / command / directory. Optional — missing/null/unknown defaults to none (forward compat).</summary>
    [JsonPropertyName("cardsGroupKey")]
    public string CardsGroupKey { get; init; } = LauncherCardsGroupKeys.None;

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
