using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed record LaunchEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = "";

    [JsonPropertyName("directory")]
    [JsonRequired]
    public string Directory { get; set; } = "";

    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; set; } = "";

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = "";
}
