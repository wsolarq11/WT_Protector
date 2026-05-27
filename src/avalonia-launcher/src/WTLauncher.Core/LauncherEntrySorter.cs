namespace WTLauncher.Core;

public static class LauncherEntrySorter
{
    public static IEnumerable<T> Sort<T>(
        IEnumerable<T> entries,
        string? sortMode,
        List<string> customOrder,
        IEnumerable<string> allEntryIds)
        where T : IHasLaunchEntry
    {
        return sortMode switch
        {
            "updated" => entries
                .OrderByDescending(entry => ParseUpdatedAt(entry.Entry.UpdatedAtUtc))
                .ThenBy(entry => entry.Entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Entry.Id, StringComparer.Ordinal),
            "alphabetical" => entries
                .OrderBy(entry => entry.Entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Entry.Id, StringComparer.Ordinal),
            _ => SortByCustomOrder(entries, customOrder, allEntryIds),
        };
    }

    private static IEnumerable<T> SortByCustomOrder<T>(
        IEnumerable<T> entries,
        List<string> customOrder,
        IEnumerable<string> allEntryIds)
        where T : IHasLaunchEntry
    {
        LauncherEntryOrder.SyncWithEntries(customOrder, allEntryIds);
        var order = LauncherEntryOrder.BuildIndex(customOrder);
        return entries
            .OrderBy(entry => order.TryGetValue(entry.Entry.Id, out var index) ? index : int.MaxValue)
            .ThenBy(entry => entry.Entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Entry.Id, StringComparer.Ordinal);
    }

    private static DateTimeOffset ParseUpdatedAt(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }
}
