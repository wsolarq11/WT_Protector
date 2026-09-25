using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable runtime settings. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>. Collection properties use
/// ImmutableList/immutable-friendly types for true immutability.
/// ponytail: SchemaVersion lives only at LauncherStoreFile top level — single source of truth.
/// launcher.json uses Loose Read at load time (unknown keys silently skipped
/// for forward compatibility), so settings.schemaVersion would be ignored
/// rather than failing loudly.</summary>
public sealed record LauncherSettings
{
    [JsonPropertyName("lastFilterText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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

    [JsonPropertyName("backgroundMode")]
    public string BackgroundMode { get; init; } = "solid";

    [JsonPropertyName("quickLaunchHotkey")]
    public string QuickLaunchHotkey { get; init; } = LauncherHotkey.DefaultText;

    /// <summary>When true, closing the main window hides it to the tray instead of quitting.</summary>
    [JsonPropertyName("runInTray")]
    public bool RunInTray { get; init; } = true;

    /// <summary>Optional HTTP(S)/file URL to an update manifest. Empty disables remote checks; local publish folder manifest still works when present.</summary>
    [JsonPropertyName("updateManifestUrl")]
    public string UpdateManifestUrl { get; init; } = "";

    /// <summary>User-chosen accent color (hex, #RRGGBB). Defaults to the "11/11" entry-count badge blue.</summary>
    [JsonPropertyName("accentColor")]
    public string AccentColor { get; init; } = AccentColorRamp.DefaultAccentColor;

    /// <summary>User-managed accent palette (hex, #RRGGBB). Seeded from
    /// <see cref="AccentColorRamp.PresetColors"/>; the picker adds picked custom
    /// colors and removes unwanted swatches.</summary>
    [JsonPropertyName("accentPresets")]
    public ImmutableList<string> AccentPresets { get; init; } = ImmutableList.CreateRange(AccentColorRamp.PresetColors);

    [JsonPropertyName("window")]
    [JsonRequired]
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
