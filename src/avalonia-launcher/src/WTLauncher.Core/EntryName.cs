using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;
[JsonConverter(typeof(EntryNameJsonConverter))]
public readonly record struct EntryName
{
    private readonly string _value;

    public string Value => _value;

    private EntryName(string value) => _value = value;

    public static EntryName Empty => new(string.Empty);

    public static EntryName Parse(string? raw)
    {
        var source = raw ?? string.Empty;
        source = StripUnpairedSurrogates(source);
        var normalized = source.Trim().Normalize(NormalizationForm.FormKC);

        if (normalized.Length == 0)
            throw new FormatException("名称不能为空。");

        return new EntryName(normalized);
    }

    public static string Normalize(string? value)
    {
        try { return Parse(value).Value; }
        catch (FormatException) { return string.Empty; }
    }

    public static string RequireNonEmpty(string? value, string fieldName = "Entry name")
    {
        try { return Parse(value).Value; }
        catch (FormatException ex) { throw new InvalidOperationException($"{fieldName} is required.", ex); }
    }

    public static implicit operator string(EntryName name) => name.Value;
    public static implicit operator EntryName(string value) => Parse(value);
    public override string ToString() => Value;

    private static string StripUnpairedSurrogates(string value)
    {
        var hasUnpaired = false;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsSurrogate(value[i]))
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                { i++; continue; }
                hasUnpaired = true;
                break;
            }
        }
        if (!hasUnpaired) return value;

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            { sb.Append(value[i]); sb.Append(value[i + 1]); i++; }
            else if (!char.IsSurrogate(value[i]))
                sb.Append(value[i]);
        }
        return sb.ToString();
    }
}

public sealed class EntryNameJsonConverter : JsonConverter<EntryName>
{
    public override EntryName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return EntryName.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, EntryName value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
