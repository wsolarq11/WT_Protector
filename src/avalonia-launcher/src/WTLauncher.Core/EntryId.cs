using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VogenEntryId = WTLauncher.Core.ValueObjects.EntryId;

namespace WTLauncher.Core;

[JsonConverter(typeof(EntryIdJsonConverter))]
public readonly record struct EntryId
{
    private readonly VogenEntryId _vogen;

    public string Value => _vogen.Value;

    private EntryId(VogenEntryId vogen) => _vogen = vogen;

    public static EntryId Empty => new(VogenEntryId.Empty);

    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    public static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static EntryId Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("ID 不能为空。");

        var trimmed = raw.Trim().Normalize(NormalizationForm.FormC);

        const int maxLength = 256;
        if (trimmed.Length > maxLength)
        {
            var span = trimmed.AsSpan(0, maxLength);
            if (char.IsHighSurrogate(span[^1]))
                span = trimmed.AsSpan(0, maxLength + 1);
            trimmed = span.ToString();
        }

        trimmed = StripUnsafeChars(trimmed);

        if (trimmed.Length == 0)
            throw new FormatException("ID 仅包含无效字符。");

        if (WindowsReservedNames.Contains(trimmed))
            throw new FormatException($"ID \"{trimmed}\" 是 Windows 保留名称。");

        if (trimmed.Contains('\\') || trimmed.Contains('/') || trimmed.Contains(".."))
            throw new FormatException($"ID 包含路径分隔符或 \"..\"。");

        return new EntryId(VogenEntryId.From(trimmed));
    }

    public static string Normalize(string? value)
    {
        try { return Parse(value).Value; }
        catch (FormatException) { return string.Empty; }
    }

    public static bool Same(string? left, string? right)
        => string.Equals(left, right, StringComparison.Ordinal);

    public static bool SameNormalized(string? left, string? right)
    {
        try
        {
            var l = Parse(left);
            var r = Parse(right);
            return Same(l.Value, r.Value);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    public static List<string> ToRequiredUniqueList(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);
        List<string> list = [];
        var seen = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            string normalized;
            try { normalized = Parse(id).Value; }
            catch (FormatException ex) { throw new ArgumentException(ex.Message, paramName); }
            if (!seen.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);
            list.Add(normalized);
            index++;
        }
        return list;
    }

    public static HashSet<string> ToRequiredSet(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var set = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            string normalized;
            try { normalized = Parse(id).Value; }
            catch (FormatException ex) { throw new ArgumentException(ex.Message, paramName); }
            if (!set.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);
            index++;
        }
        return set;
    }

    public static string RequireEntryId(string? id, string paramName)
    {
        try { return Parse(id).Value; }
        catch (FormatException ex) { throw new ArgumentException(ex.Message, paramName); }
    }

    public static implicit operator EntryId(string value) => Parse(value);
    public static implicit operator string(EntryId id) => id.Value;
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

public sealed class EntryIdJsonConverter : JsonConverter<EntryId>
{
    public override EntryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return EntryId.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, EntryId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
