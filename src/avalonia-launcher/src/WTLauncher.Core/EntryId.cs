using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// A validated and normalized entry ID. Construction through <see cref="TryParse"/> is the
/// ONLY way to create an instance — the private constructor ensures invalid IDs cannot exist.
/// FormC normalization, control-character stripping, Windows-reserved-name rejection,
/// path-separator rejection, and 256-char truncation all happen at construction time.
/// </summary>
[JsonConverter(typeof(EntryIdJsonConverter))]
public readonly record struct EntryId
{
    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    internal static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public string Value { get; }

    private EntryId(string value) => Value = value;

    public static Result<EntryId, IdError> TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new IdError.Empty();

        var trimmed = raw.Trim().Normalize(NormalizationForm.FormC);

        // Truncate at code-point boundary
        const int maxLength = 256;
        if (trimmed.Length > maxLength)
        {
            var span = trimmed.AsSpan(0, maxLength);
            if (char.IsHighSurrogate(span[^1]))
                span = trimmed.AsSpan(0, maxLength + 1);
            trimmed = span.ToString();
        }

        // Strip control and format characters
        trimmed = StripUnsafeChars(trimmed);

        if (trimmed.Length == 0)
            return new IdError.Empty();

        if (WindowsReservedNames.Contains(trimmed))
            return new IdError.ReservedName(trimmed);

        if (trimmed.Contains('\\') || trimmed.Contains('/') || trimmed.Contains(".."))
            return new IdError.PathSeparator(trimmed);

        return new EntryId(trimmed);
    }

    /// <summary>Ordinal equality comparison. Both sides must already be normalized.</summary>
    public static bool Same(string? left, string? right)
        => string.Equals(left, right, StringComparison.Ordinal);

    /// <summary>Normalizes both sides then compares with ordinal equality.</summary>
    public static bool SameNormalized(string? left, string? right)
        => Same(TryParse(left).Match(ok: e => e.Value, err: _ => string.Empty),
                TryParse(right).Match(ok: e => e.Value, err: _ => string.Empty));

    /// <summary>Returns true when the ID is not null, empty, or whitespace.</summary>
    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    /// <summary>Validates, normalizes, and deduplicates a sequence of entry IDs.</summary>
    public static List<string> ToRequiredUniqueList(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);
        List<string> list = [];
        var seen = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            var result = TryParse(id);
            var normalized = result.Match(ok: e => e.Value, err: _ =>
                throw new ArgumentException($"Entry id {index} is invalid.", paramName));
            if (!seen.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);
            list.Add(normalized);
            index++;
        }
        return list;
    }

    /// <summary>Validates, normalizes, and deduplicates entry IDs into a set.</summary>
    public static HashSet<string> ToRequiredSet(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var set = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            var result = TryParse(id);
            var normalized = result.Match(ok: e => e.Value, err: _ =>
                throw new ArgumentException($"Entry id {index} is invalid.", paramName));
            if (!set.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);
            index++;
        }
        return set;
    }

    /// <summary>Requires a valid entry ID, throwing on failure. For backward compatibility
    /// with existing APIs that expect exceptions rather than Results.</summary>
    public static string RequireEntryId(string? id, string paramName)
    {
        var result = TryParse(id);
        return result.Match(
            ok: e => e.Value,
            err: error => throw new ArgumentException(error.Message, paramName));
    }

    public override string ToString() => Value;

    private static string StripUnsafeChars(string value)
    {
        var hasUnsafe = false;
        for (var i = 0; i < value.Length; i++)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(value[i]);
            if (uc is UnicodeCategory.Control or UnicodeCategory.Format)
            { hasUnsafe = true; break; }
        }
        if (!hasUnsafe) return value;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc is not UnicodeCategory.Control and not UnicodeCategory.Format)
                sb.Append(ch);
        }
        return sb.ToString();
    }
}

/// <summary>JSON converter: EntryId ↔ JSON string (transparent serialization).</summary>
public sealed class EntryIdJsonConverter : JsonConverter<EntryId>
{
    public override EntryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = EntryId.TryParse(raw);
        return result.Match(
            ok: id => id,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, EntryId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
