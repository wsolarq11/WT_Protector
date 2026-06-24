using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable preset container. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>, making every state change explicit.</summary>
public sealed record LauncherPresetFile
{
    [JsonPropertyName("schemaVersion")]
    [JsonRequired]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("defaultPresetId")]
    [JsonRequired]
    public string DefaultPresetId { get; init; } = "";

    [JsonPropertyName("presets")]
    [JsonRequired]
    public ImmutableList<LauncherPreset> Presets { get; init; } = ImmutableList<LauncherPreset>.Empty;
}

/// <summary>Immutable preset entry. All properties are init-only — mutations
/// produce a new instance via <c>with { }</c>.</summary>
public sealed record LauncherPreset
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; } = "";

    [JsonPropertyName("entryIds")]
    [JsonRequired]
    public ImmutableList<string> EntryIds { get; init; } = ImmutableList<string>.Empty;

    [JsonPropertyName("locked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Locked { get; init; }
}
