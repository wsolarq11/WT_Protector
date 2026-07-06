using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public readonly record struct PresetName
{
    public const int MaxLength = 256;

    public string Value { get; }

    private PresetName(string value) => Value = value;

    public static PresetName Parse(string? raw)
    {
        var entryName = EntryName.Parse(raw);
        var value = entryName.Value;

        if (value.Length > MaxLength)
            throw new FormatException($"名称长度 {value.Length} 超过 {MaxLength} 字符限制。");

        return new PresetName(value);
    }

    public static implicit operator string(PresetName name) => name.Value;
    public override string ToString() => Value;
}

public sealed class PresetNameJsonConverter : JsonConverter<PresetName>
{
    public override PresetName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return PresetName.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, PresetName value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
