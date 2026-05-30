namespace WTLauncher.Core;

public static class LauncherEntryVisibility
{
    public const string CustomSortModeId = "custom";
    public const string UpdatedSortModeId = "updated";
    public const string AlphabeticalSortModeId = "alphabetical";

    public static string NormalizeSortModeId(string? sortModeId)
    {
        var normalized = (sortModeId ?? string.Empty).Trim();
        return normalized switch
        {
            UpdatedSortModeId => UpdatedSortModeId,
            AlphabeticalSortModeId => AlphabeticalSortModeId,
            _ => CustomSortModeId,
        };
    }

    public static IReadOnlyList<TEntry> FilterAndSort<TEntry>(
        IEnumerable<TEntry> entries,
        string? filterText,
        string? sortModeId,
        IEnumerable<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(selectEntry);

        var source = entries.ToList();
        var normalizedFilter = (filterText ?? string.Empty).Trim();
        var filtered = normalizedFilter.Length == 0
            ? source
            : source.Where(entry => MatchesFilter(selectEntry(entry), normalizedFilter));

        var sorted = NormalizeSortModeId(sortModeId) switch
        {
            UpdatedSortModeId => SortByUpdated(filtered, selectEntry),
            AlphabeticalSortModeId => SortAlphabetically(filtered, selectEntry),
            _ => SortByCustomOrder(filtered, customEntryOrder, selectEntry),
        };

        return sorted.ToList();
    }

    private static bool MatchesFilter(LaunchEntry entry, string filterText) =>
        Text(entry.Name).Contains(filterText, StringComparison.OrdinalIgnoreCase)
        || Text(entry.Directory).Contains(filterText, StringComparison.OrdinalIgnoreCase)
        || Text(entry.Command).Contains(filterText, StringComparison.OrdinalIgnoreCase);

    private static IOrderedEnumerable<TEntry> SortByUpdated<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        ThenByNameThenId(
            entries.OrderByDescending(entry => ParseUpdatedAt(selectEntry(entry).UpdatedAtUtc)),
            selectEntry);

    private static IOrderedEnumerable<TEntry> SortAlphabetically<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries.OrderBy(entry => Text(selectEntry(entry).Name), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => selectEntry(entry).Id, LauncherEntryIds.Comparer);

    private static IOrderedEnumerable<TEntry> SortByCustomOrder<TEntry>(
        IEnumerable<TEntry> entries,
        IEnumerable<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        var order = LauncherEntryOrder.BuildIndex(customEntryOrder);
        return ThenByNameThenId(
            entries.OrderBy(entry => order.TryGetValue(LauncherEntryIds.Normalize(selectEntry(entry).Id), out var index) ? index : int.MaxValue),
            selectEntry);
    }

    private static IOrderedEnumerable<TEntry> ThenByNameThenId<TEntry>(
        IOrderedEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries
            .ThenBy(entry => Text(selectEntry(entry).Name), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => selectEntry(entry).Id, LauncherEntryIds.Comparer);

    private static string Text(string? value) => value ?? string.Empty;

    private static DateTimeOffset ParseUpdatedAt(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
}
