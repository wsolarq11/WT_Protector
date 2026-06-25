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

    /// <summary>
    /// Normalizes a string value and throws <see cref="ArgumentException"/> if the
    /// result is empty. Unified entry-point for all "entry name is required" guards
    /// across the ViewModel layer so exception type and message are consistent.
    /// </summary>
    public static string RequireNonEmpty(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paramName);
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            throw new ArgumentException("Value must not be empty.", paramName);
        return normalized;
    }
}
