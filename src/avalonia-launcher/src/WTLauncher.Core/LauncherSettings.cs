using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed class LauncherSettings
{
    public const string DefaultTheme = "light";

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = DefaultTheme;

    [JsonPropertyName("lastFilterText")]
    public string LastFilterText { get; set; } = "";

    [JsonPropertyName("sortMode")]
    public string SortMode { get; set; } = LauncherEntryVisibility.CustomSortModeId;

    [JsonPropertyName("entryViewMode")]
    public string EntryViewMode { get; set; } = LauncherEntryViewModes.CardsId;

    [JsonPropertyName("requireConfirmationForLaunches")]
    public bool RequireConfirmationForLaunches { get; set; } = true;

    [JsonPropertyName("customEntryOrder")]
    public List<string> CustomEntryOrder { get; set; } = [];

    [JsonPropertyName("window")]
    public LauncherWindowSettings Window { get; set; } = new();
}

public sealed class LauncherWindowSettings
{
    public const double DefaultWidth = 1180;

    public const double DefaultHeight = 760;

    [JsonPropertyName("width")]
    public double Width { get; set; } = DefaultWidth;

    [JsonPropertyName("height")]
    public double Height { get; set; } = DefaultHeight;
}
