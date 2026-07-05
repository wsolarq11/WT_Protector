using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// A validated and normalized preset ID. FormC normalization, lowercase conversion,
/// alphanumeric/separator filtering, and 256-char truncation happen at construction time.
/// </summary>
[JsonConverter(typeof(PresetIdJsonConverter))]
public readonly record struct PresetId
{
    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    public string Value { get; }

    private PresetId(string value) => Value = value;

    public static Result<PresetId, IdError> TryParse(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();

        // Truncate at code-point boundary
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

        return new PresetId(normalized);
    }

    /// <summary>Normalizes a preset ID. For backward compatibility.</summary>
    public static string Normalize(string? value)
    {
        var result = TryParse(value);
        return result.Match(ok: p => p.Value, err: _ => "preset");
    }

    /// <summary>Normalizes a reference that may be empty (optional).</summary>
    public static string NormalizeReference(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : Normalize(value);

    /// <summary>Ordinal equality comparison.</summary>
    public static bool Same(string? left, string? right)
        => string.Equals(left, right, StringComparison.Ordinal);

    /// <summary>
    /// Generates a unique preset ID from a name, avoiding collisions with existing IDs.
    /// </summary>
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
    public override string ToString() => Value;
}

public sealed class PresetIdJsonConverter : JsonConverter<PresetId>
{
    public override PresetId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = PresetId.TryParse(raw);
        return result.Match(
            ok: id => id,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, PresetId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
