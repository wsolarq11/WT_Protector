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

        // Pinyin: romanized Chinese name/directory also matches (english keyboard,
        // no IME switch). Full pinyin and initials both count.
        if (HanziPinyin.ContainsHan(name) || HanziPinyin.ContainsHan(directory))
        {
            var nameFull = HanziPinyin.ToPinyin(name).Replace(" ", string.Empty);
            var nameInit = HanziPinyin.ToInitials(name);
            if (nameFull.StartsWith(q, StringComparison.OrdinalIgnoreCase)
                || nameInit.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                score += 400;
            else if (nameFull.Contains(q, StringComparison.OrdinalIgnoreCase)
                || nameInit.Contains(q, StringComparison.OrdinalIgnoreCase))
                score += 200;

            var dirFull = HanziPinyin.ToPinyin(directory).Replace(" ", string.Empty);
            var dirInit = HanziPinyin.ToInitials(directory);
            if (dirFull.Contains(q, StringComparison.OrdinalIgnoreCase)
                || dirInit.Contains(q, StringComparison.OrdinalIgnoreCase))
                score += 150;
        }

        // Typo tolerance: reward a small edit distance when query and name are
        // close in length. Short queries stay prefix-based.
        if (q.Length >= 3 && Math.Abs(q.Length - name.Length) <= 2)
        {
            var distance = EditDistance(q, name);
            if (distance is >= 1 and <= 2)
                score += 250 - distance * 100; // distance 1 → 150, distance 2 → 50
        }

        return score;
    }

    /// <summary>Case-insensitive Levenshtein distance between two strings.</summary>
    private static int EditDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = char.ToUpperInvariant(a[i - 1]) == char.ToUpperInvariant(b[j - 1]) ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
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
