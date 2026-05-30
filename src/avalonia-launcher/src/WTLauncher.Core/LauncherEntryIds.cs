namespace WTLauncher.Core;

public static class LauncherEntryIds
{
    public static StringComparer Comparer => StringComparer.Ordinal;

    public static string Normalize(string? id) => (id ?? string.Empty).Trim();

    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    public static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    public static bool SameNormalized(string? left, string? right) =>
        Same(Normalize(left), Normalize(right));

    public static List<string> ToUniqueUsableList(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var list = new List<string>();
        var seen = new HashSet<string>(Comparer);
        foreach (var id in ids)
        {
            var normalized = Normalize(id);
            if (IsUsable(normalized) && seen.Add(normalized))
                list.Add(normalized);
        }

        return list;
    }

    public static HashSet<string> ToUsableSet(IEnumerable<string> ids) =>
        ToUniqueUsableList(ids).ToHashSet(Comparer);
}
