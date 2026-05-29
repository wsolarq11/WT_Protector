namespace WTLauncher.Core;

public static class LauncherEntryVisibility
{
    public const string CustomSortModeId = "custom";
    public const string UpdatedSortModeId = "updated";
    public const string AlphabeticalSortModeId = "alphabetical";

    public static IReadOnlyList<TEntry> FilterAndSort<TEntry>(
        IEnumerable<TEntry> entries,
        string? filterText,
        string? sortModeId,
        List<string> customEntryOrder,
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

        var sorted = sortModeId switch
        {
            UpdatedSortModeId => SortByUpdated(filtered, selectEntry),
            AlphabeticalSortModeId => SortAlphabetically(filtered, selectEntry),
            _ => SortByCustomOrder(filtered, source, customEntryOrder, selectEntry),
        };

        return sorted.ToList();
    }

    private static bool MatchesFilter(LaunchEntry entry, string filterText) =>
        entry.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
        || entry.Directory.Contains(filterText, StringComparison.OrdinalIgnoreCase)
        || entry.Command.Contains(filterText, StringComparison.OrdinalIgnoreCase);

    private static IOrderedEnumerable<TEntry> SortByUpdated<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        ThenByNameThenId(
            entries.OrderByDescending(entry => ParseUpdatedAt(selectEntry(entry).UpdatedAtUtc)),
            selectEntry);

    private static IOrderedEnumerable<TEntry> SortAlphabetically<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries.OrderBy(entry => selectEntry(entry).Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => selectEntry(entry).Id, LauncherEntryIds.Comparer);

    private static IOrderedEnumerable<TEntry> SortByCustomOrder<TEntry>(
        IEnumerable<TEntry> entries,
        IEnumerable<TEntry> allEntries,
        List<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        LauncherEntryOrder.SyncWithEntries(customEntryOrder, allEntries.Select(entry => selectEntry(entry).Id));
        var order = LauncherEntryOrder.BuildIndex(customEntryOrder);
        return ThenByNameThenId(
            entries.OrderBy(entry => order.TryGetValue(selectEntry(entry).Id, out var index) ? index : int.MaxValue),
            selectEntry);
    }

    private static IOrderedEnumerable<TEntry> ThenByNameThenId<TEntry>(
        IOrderedEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries
            .ThenBy(entry => selectEntry(entry).Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => selectEntry(entry).Id, LauncherEntryIds.Comparer);

    private static DateTimeOffset ParseUpdatedAt(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
}
