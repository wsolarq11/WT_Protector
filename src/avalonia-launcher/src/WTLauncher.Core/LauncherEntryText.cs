using System.Text;

namespace WTLauncher.Core;

public static class LauncherEntryText
{
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
