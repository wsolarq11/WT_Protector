using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed record LaunchEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("directory")]
    public string Directory { get; set; } = "";

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("confirm")]
    public bool Confirm { get; set; } = true;

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = "";
}
