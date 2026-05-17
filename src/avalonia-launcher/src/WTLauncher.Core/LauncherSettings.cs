using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed class LauncherSettings
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "light";

    [JsonPropertyName("lastFilterText")]
    public string LastFilterText { get; set; } = "";

    [JsonPropertyName("window")]
    public LauncherWindowSettings Window { get; set; } = new();
}

public sealed class LauncherWindowSettings
{
    [JsonPropertyName("width")]
    public double Width { get; set; } = 1180;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 760;
}
