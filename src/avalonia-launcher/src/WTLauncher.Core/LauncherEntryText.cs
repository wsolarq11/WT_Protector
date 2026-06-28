using System.Text;

namespace WTLauncher.Core;

public static class LauncherEntryText
{
    /// <summary>
    /// Trims whitespace and applies Unicode FormKC normalization to produce stable,
    /// sortable text. FormKC is an intentional choice: it decomposes ligatures and
    /// maps fullwidth characters to their ASCII equivalents (e.g. fullwidth 'A' to
    /// regular 'A'), which may change visual appearance. This is acceptable because
    /// the normalized form is used for comparison, deduplication, and sorting —
    /// not for display. Note that after a load/save cycle, the on-disk JSON form of
    /// entry text will reflect the normalized value, not the original verbatim input.
    /// </summary>
    public static string Normalize(string? value)
    {
        var source = value ?? string.Empty;
        // Strip unpaired surrogates before Normalize — .NET's Normalize()
        // throws ArgumentException on lone surrogates (RFC 8259 allows them).
        source = StripUnpairedSurrogates(source);
        return source.Trim().Normalize(NormalizationForm.FormKC);
    }

    private static string StripUnpairedSurrogates(string value)
    {
        var hasUnpaired = false;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsSurrogate(value[i]))
            {
                // High surrogate must be followed by a low surrogate.
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                { i++; continue; }
                // Low surrogate must be preceded by a high surrogate (but we scan left-to-right,
                // so lone low surrogates are caught here too).
                hasUnpaired = true;
                break;
            }
        }

        if (!hasUnpaired)
            return value;

        var sb = new System.Text.StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            { sb.Append(value[i]); sb.Append(value[i + 1]); i++; }
            else if (!char.IsSurrogate(value[i]))
                sb.Append(value[i]);
        }

        return sb.ToString();
    }

    // ponytail: single shared validation replacing 5 identical private RequireEntryName/RequirePresetName copies.
    public static string RequireNonEmpty(string? value, string fieldName = "Entry name")
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            throw new InvalidOperationException($"{fieldName} is required.");
        return normalized;
    }
}
