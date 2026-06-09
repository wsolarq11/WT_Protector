using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed class LauncherSettings
{
    [JsonPropertyName("lastFilterText")]
    [JsonRequired]
    public string LastFilterText { get; set; } = "";

    [JsonPropertyName("sortMode")]
    [JsonRequired]
    public string SortMode { get; set; } = LauncherEntryVisibility.CustomSortModeId;

    [JsonPropertyName("entryViewMode")]
    [JsonRequired]
    public string EntryViewMode { get; set; } = LauncherEntryViewModes.CardsId;

    [JsonPropertyName("requireConfirmationForLaunches")]
    [JsonRequired]
    public bool RequireConfirmationForLaunches { get; set; } = true;

    [JsonPropertyName("customEntryOrder")]
    [JsonRequired]
    public List<string> CustomEntryOrder { get; set; } = [];

    [JsonPropertyName("commandTemplates")]
    public Dictionary<string, string> CommandTemplates { get; set; } = [];

    [JsonPropertyName("searchHistory")]
    public List<string> SearchHistory { get; set; } = [];

    [JsonPropertyName("window")]
    [JsonRequired]
    public LauncherWindowSettings Window { get; set; } = new();
}

public sealed class LauncherWindowSettings
{
    public const double DefaultWidth = 1180;

    public const double DefaultHeight = 760;

    [JsonPropertyName("width")]
    [JsonRequired]
    public double Width { get; set; } = DefaultWidth;

    [JsonPropertyName("height")]
    [JsonRequired]
    public double Height { get; set; } = DefaultHeight;
}
