using System.Text;

namespace WTLauncher.Core;

public static class LauncherEntryIds
{
    public static StringComparer Comparer => StringComparer.Ordinal;

    public static string Normalize(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        var trimmed = id.Trim();
        // Consistent with LauncherPresetIds.Normalize (both use 256-character limit).
        if (trimmed.Length > 256)
            trimmed = trimmed.Substring(0, 256);

        // Fast path: no control characters.
        var hasControl = false;
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (char.IsControl(trimmed[i]))
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
            if (!char.IsControl(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }

    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    public static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

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
    public static string RequireEntryId(string? id, string paramName)
    {
        var normalized = Normalize(id);
        if (!IsUsable(normalized))
            throw new ArgumentException("Entry id is required.", paramName);

        return normalized;
    }

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
