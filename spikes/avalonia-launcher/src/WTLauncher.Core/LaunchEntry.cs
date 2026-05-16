using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed record LaunchEntry
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("directory")]
    public string Directory { get; init; } = "";

    [JsonPropertyName("command")]
    public string Command { get; init; } = "";

    [JsonPropertyName("confirm")]
    public bool Confirm { get; init; } = true;
}
