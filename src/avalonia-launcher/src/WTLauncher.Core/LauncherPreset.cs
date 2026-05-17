using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed record LauncherPresetFile
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("defaultPresetId")]
    public string DefaultPresetId { get; set; } = "";

    [JsonPropertyName("presets")]
    public List<LauncherPreset> Presets { get; set; } = [];
}

public sealed record LauncherPreset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("entryIds")]
    public List<string> EntryIds { get; set; } = [];
}
