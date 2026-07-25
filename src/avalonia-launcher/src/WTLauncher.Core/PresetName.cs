using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;
[JsonConverter(typeof(PresetNameJsonConverter))]
public readonly record struct PresetName
{
    private readonly string _value;

    public string Value => _value;

    private PresetName(string value) => _value = value;

    public static PresetName Empty => new(string.Empty);

    public const int MaxLength = 256;

    public static PresetName Parse(string? raw)
    {
        var entryName = EntryName.Parse(raw);
        var value = entryName.Value;

        if (value.Length > MaxLength)
            throw new FormatException($"名称长度 {value.Length} 超过 {MaxLength} 字符限制。");

        return new PresetName(value);
    }

    public static implicit operator string(PresetName name) => name.Value;
    public static implicit operator PresetName(string value) => Parse(value);
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
