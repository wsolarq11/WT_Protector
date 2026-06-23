using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable data carrier. All properties are init-only — every state
/// change produces a new instance via <c>with { }</c>, making mutations explicit,
/// traceable, and impossible to miss. No invisible shared-mutation bugs.</summary>
public sealed record LaunchEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; } = "";

    [JsonPropertyName("directory")]
    [JsonRequired]
    public string Directory { get; init; } = "";

    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; init; } = "";

    [JsonPropertyName("selected")]
    public bool Selected { get; init; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; init; } = "";

    [JsonPropertyName("launchedAtUtc")]
    public string LaunchedAtUtc { get; init; } = "";
}
