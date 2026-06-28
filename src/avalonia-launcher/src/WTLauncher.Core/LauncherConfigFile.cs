using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable config container. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>.</summary>
public sealed record LauncherConfigFile
{
    [JsonPropertyName("schemaVersion")]
    [JsonRequired]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("entries")]
    [JsonRequired]
    public ImmutableList<LaunchEntry> Entries { get; init; } = ImmutableList<LaunchEntry>.Empty;
}
