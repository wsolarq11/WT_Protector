using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Single-file store container replacing the three separate
/// config.json / settings.json / presets.json files. All properties are
/// init-only — mutations produce a new instance via <c>with { }</c>.</summary>
public sealed record LauncherStoreFile
{
    [JsonPropertyName("schemaVersion")]
    [JsonRequired]
    public int SchemaVersion { get; init; } = 2;

    [JsonPropertyName("settings")]
    [JsonRequired]
    public LauncherSettings Settings { get; init; } = new();

    [JsonPropertyName("entries")]
    [JsonRequired]
    public ImmutableList<LaunchEntry> Entries { get; init; } = ImmutableList<LaunchEntry>.Empty;

    [JsonPropertyName("presets")]
    [JsonRequired]
    public ImmutableList<LauncherPreset> Presets { get; init; } = ImmutableList<LauncherPreset>.Empty;

    [JsonPropertyName("defaultPresetId")]
    [JsonRequired]
    public string DefaultPresetId { get; init; } = "";
}
