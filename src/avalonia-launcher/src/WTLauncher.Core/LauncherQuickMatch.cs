namespace WTLauncher.Core;

/// <summary>
/// Lightweight quick-launch ranking. This is intentionally not a full fuzzy
/// search engine; it rewards prefix > substring > directory > command and adds
/// a subsequence bonus for loose typing.
/// </summary>
public static class LauncherQuickMatch
{
    public static bool IsMatch(string? query, LaunchEntry entry)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;
        return Score(query, entry) > 0;
    }

    public static int Score(string? query, LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var q = query.Trim();
        if (q.Length == 0)
            return 0;

        var name = entry.Name.Value;
        var directory = entry.Directory.Value;
        var command = entry.Command;
        var score = 0;

        if (name.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            score += 1000;
        else if (name.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 500;

        if (directory.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 300;
        if (command.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 100;
        if (IsSubsequence(q, name))
            score += 20;

        return score;
    }

    internal static bool IsSubsequence(string query, string candidate)
    {
        if (query.Length == 0)
            return true;

        var queryIndex = 0;
        foreach (var ch in candidate)
        {
            if (char.ToUpperInvariant(ch) == char.ToUpperInvariant(query[queryIndex]))
            {
                queryIndex++;
                if (queryIndex == query.Length)
                    return true;
            }
        }

        return false;
    }
}
