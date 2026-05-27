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

    /// <summary>
    /// Legacy compatibility field. Runtime launch confirmation is controlled by
    /// the global <c>requireConfirmationForLaunches</c> setting in settings.json.
    /// This property is retained only for JSON backward compatibility and is not
    /// used by <see cref="LaunchArgumentBuilder.Build"/>.
    /// </summary>
    [JsonPropertyName("confirm")]
    public bool Confirm { get; set; } = true;

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = "";
}
