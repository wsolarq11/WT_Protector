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
    public static string Normalize(string? value) => (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC);

    // ponytail: single shared validation replacing 5 identical private RequireEntryName/RequirePresetName copies.
    public static string RequireNonEmpty(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Entry name is required.");
        return normalized;
    }
}
