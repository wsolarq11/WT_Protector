using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VogenPresetId = WTLauncher.Core.ValueObjects.PresetId;

namespace WTLauncher.Core;
[JsonConverter(typeof(PresetIdJsonConverter))]
public readonly record struct PresetId
{
    private readonly VogenPresetId _vogen;

    public string Value => _vogen.Value;

    private PresetId(VogenPresetId vogen) => _vogen = vogen;

    public static PresetId Empty => new(VogenPresetId.Empty);

    public const int MaxIdLength = 256;

    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    public static PresetId Parse(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();

        const int maxLength = 256;
        if (value.Length > maxLength)
        {
            var span = value.AsSpan(0, maxLength);
            if (char.IsHighSurrogate(span[^1]) && value.Length >= maxLength + 1)
                span = value.AsSpan(0, maxLength + 1);
            value = span.ToString();
        }

        var builder = new StringBuilder(value.Length);
        var separatorPending = false;
        foreach (var ch in value.Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                separatorPending = false;
            }
            else if (ch is '-' or '_')
            {
                builder.Append(ch);
                separatorPending = false;
            }
            else if (char.IsWhiteSpace(ch) || ch is '/' or '\\' or '.' or ':')
            {
                if (!separatorPending && builder.Length > 0)
                { builder.Append('-'); separatorPending = true; }
            }
        }

        var normalized = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "preset";

        return new PresetId(VogenPresetId.From(normalized));
    }

    public static string Normalize(string? value)
        => Parse(value).Value;

    public static string NormalizeReference(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : Normalize(value);

    public static bool Same(string? left, string? right)
        => string.Equals(left, right, StringComparison.Ordinal);

    public static string GenerateUnique(string name, IEnumerable<string> existingIds)
    {
        ArgumentNullException.ThrowIfNull(existingIds);
        var baseId = Normalize(name);
        var candidate = baseId;
        var index = 2;
        const int maxAttempts = 10000;
        var existing = existingIds.Select(Normalize).ToHashSet(Comparer);
        while (existing.Contains(candidate))
        {
            if (index > maxAttempts)
                throw new InvalidOperationException("Cannot generate a unique preset ID.");
            candidate = $"{baseId}-{index++}";
        }
        return candidate;
    }

    public static implicit operator string(PresetId id) => id.Value;
    public static implicit operator PresetId(string value) => Parse(value);
    public override string ToString() => Value;
}

public sealed class PresetIdJsonConverter : JsonConverter<PresetId>
{
    public override PresetId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return PresetId.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, PresetId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
