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

    public static HashSet<string> ToUsableSet(IEnumerable<string> ids) =>
        ids
            .Select(Normalize)
            .Where(IsUsable)
            .ToHashSet(Comparer);
}
