using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// A validated and normalized entry name. FormKC normalization and unpaired-surrogate
/// stripping happen at construction time. The private constructor ensures invalid
/// names cannot exist.
/// </summary>
[JsonConverter(typeof(EntryNameJsonConverter))]
public readonly record struct EntryName
{
    public string Value { get; }

    private EntryName(string value) => Value = value;

    public static Result<EntryName, NameError> TryParse(string? raw)
    {
        var source = raw ?? string.Empty;
        source = StripUnpairedSurrogates(source);
        var normalized = source.Trim().Normalize(NormalizationForm.FormKC);

        if (normalized.Length == 0)
            return new NameError.Empty();

        return new EntryName(normalized);
    }

    /// <summary>Normalizes and returns the string value. For backward compatibility.</summary>
    public static string Normalize(string? value)
    {
        var result = TryParse(value);
        return result.Match(ok: e => e.Value, err: _ => string.Empty);
    }

    /// <summary>Requires a non-empty name, throwing on failure.</summary>
    public static string RequireNonEmpty(string? value, string fieldName = "Entry name")
    {
        var result = TryParse(value);
        return result.Match(
            ok: e => e.Value,
            err: _ => throw new InvalidOperationException($"{fieldName} is required."));
    }

    public static implicit operator string(EntryName name) => name.Value;
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
        var result = EntryName.TryParse(raw);
        return result.Match(
            ok: name => name,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, EntryName value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
