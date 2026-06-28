using System.Globalization;
using System.Text;

namespace WTLauncher.Core;

public static class LauncherEntryIds
{
    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    /// <summary>
    /// Applies Unicode FormC normalization and strips control/format characters,
    /// truncating to 256 code points. Returns <see cref="string.Empty"/> for null/empty input.
    /// FormC (not FormKC) is used — homoglyph attacks are not mitigated.
    /// </summary>
    public static string Normalize(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        // Apply Unicode canonical composition (FormC) so that semantically identical
        // IDs expressed in different normalization forms (NFC vs NFD) are treated as
        // the same ID. Without this, "café" (NFC) and "café" (NFD) would
        // be different IDs, breaking preset cross-references and custom sort order.
        var trimmed = id.Trim().Normalize(NormalizationForm.FormC);
        // Consistent with LauncherPresetIds.Normalize (both use 256-character limit).
        const int MaxIdLength = 256;
        if (trimmed.Length > MaxIdLength)
        {
            // Truncate at a code-point boundary to avoid splitting a surrogate pair,
            // which would produce an illegal UTF-16 string.
            var span = trimmed.AsSpan(0, MaxIdLength);
            if (char.IsHighSurrogate(span[^1]))
                span = trimmed.AsSpan(0, MaxIdLength + 1);
            trimmed = span.ToString();
        }

        // Fast path: no control characters.
        var hasControl = false;
        for (var i = 0; i < trimmed.Length; i++)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(trimmed[i]);
            if (uc is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                hasControl = true;
                break;
            }
        }

        if (!hasControl)
            return trimmed;

        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc is not UnicodeCategory.Control and not UnicodeCategory.Format)
                sb.Append(ch);
        }

        return sb.ToString();
    }

    /// <summary>Returns true when the ID is not null, empty, or whitespace.</summary>
    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    /// <summary>Ordinal equality comparison. Callers MUST already normalize both sides.</summary>
    public static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    /// <summary>Normalizes both sides then compares with ordinal equality.</summary>
    public static bool SameNormalized(string? left, string? right) =>
        Same(Normalize(left), Normalize(right));

    /// <summary>
    /// Validates and normalizes an entry ID, rejecting null, empty, or whitespace values.
    /// This is the single authoritative entry-ID validation used across the entire codebase.
    /// </summary>
    /// <param name="id">The raw entry ID to validate. Null is rejected.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>The normalized (trimmed) entry ID.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null, empty, or whitespace.</exception>
    internal static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static string RequireEntryId(string? id, string paramName)
    {
        var normalized = Normalize(id);
        if (!IsUsable(normalized))
            throw new ArgumentException("Entry id is required.", paramName);

        if (WindowsReservedNames.Contains(normalized))
            throw new ArgumentException($"Entry id '{normalized}' is a Windows reserved name.", paramName);

        // Reject path separators and parent-directory tokens to prevent
        // path-traversal attacks when entry IDs are used in file paths.
        if (normalized.Contains('\\') || normalized.Contains('/') || normalized.Contains(".."))
            throw new ArgumentException($"Entry id '{normalized}' contains path separators or '..'.", paramName);

        return normalized;
    }

    /// <summary>Validates, normalizes, and deduplicates a sequence of entry IDs, returning a list.</summary>
    public static List<string> ToRequiredUniqueList(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);

        List<string> list = [];
        var seen = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            var normalized = Normalize(id);
            if (!IsUsable(normalized))
                throw new ArgumentException($"Entry id {index} is required.", paramName);

            if (!seen.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);

            list.Add(normalized);
            index++;
        }

        return list;
    }

    /// <summary>Validates, normalizes, and deduplicates a sequence of entry IDs, returning a set.</summary>
    public static HashSet<string> ToRequiredSet(IEnumerable<string> ids, string paramName)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var set = new HashSet<string>(Comparer);
        var index = 0;
        foreach (var id in ids)
        {
            var normalized = Normalize(id);
            if (!IsUsable(normalized))
                throw new ArgumentException($"Entry id {index} is required.", paramName);

            if (!set.Add(normalized))
                throw new ArgumentException($"Entry id {index} is duplicate: {normalized}.", paramName);

            index++;
        }

        return set;
    }
}
