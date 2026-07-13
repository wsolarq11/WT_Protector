using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherSearchHistory
{
    public const int MaximumTerms = 20;

    public static ImmutableList<string> Record(
        ImmutableList<string> history,
        string term)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(term);

        if (string.IsNullOrWhiteSpace(term))
            return history;

        var normalized = EntryName.Normalize(term);
        if (history.Count > 0 &&
            string.Equals(history[0], normalized, StringComparison.OrdinalIgnoreCase))
        {
            return history;
        }

        var updated = history
            .RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase))
            .Insert(0, normalized);

        return updated.Count > MaximumTerms
            ? updated.RemoveRange(MaximumTerms, updated.Count - MaximumTerms)
            : updated;
    }
}
