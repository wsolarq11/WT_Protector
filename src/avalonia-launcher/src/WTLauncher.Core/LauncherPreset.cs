using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public sealed class LauncherPresetFile
{
    [JsonPropertyName("schemaVersion")]
    [JsonRequired]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("defaultPresetId")]
    [JsonRequired]
    public string DefaultPresetId { get; set; } = "";

    [JsonPropertyName("presets")]
    [JsonRequired]
    public List<LauncherPreset> Presets { get; set; } = [];

    public LauncherPresetFile Clone()
    {
        var clone = new LauncherPresetFile
        {
            SchemaVersion = SchemaVersion,
            DefaultPresetId = DefaultPresetId,
            Presets = Presets.Select(p => p.Clone()).ToList(),
        };
        return clone;
    }
}

public sealed class LauncherPreset
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = "";

    [JsonPropertyName("entryIds")]
    [JsonRequired]
    public List<string> EntryIds { get; set; } = [];

    [JsonPropertyName("locked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Locked { get; set; }

    public LauncherPreset Clone() => new()
    {
        Id = Id,
        Name = Name,
        EntryIds = [.. EntryIds],
        Locked = Locked,
    };
}
