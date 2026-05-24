using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

internal static class LauncherEntrySorter
{
    public static IEnumerable<LauncherEntryViewModel> Sort(
        IEnumerable<LauncherEntryViewModel> entries,
        string? sortMode,
        List<string> customOrder,
        IEnumerable<string> allEntryIds)
    {
        return sortMode switch
        {
            "updated" => entries
                .OrderByDescending(entry => ParseUpdatedAt(entry.Entry.UpdatedAtUtc))
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal),
            "alphabetical" => entries
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal),
            _ => SortByCustomOrder(entries, customOrder, allEntryIds),
        };
    }

    private static IEnumerable<LauncherEntryViewModel> SortByCustomOrder(
        IEnumerable<LauncherEntryViewModel> entries,
        List<string> customOrder,
        IEnumerable<string> allEntryIds)
    {
        LauncherEntryOrder.SyncWithEntries(customOrder, allEntryIds);
        var order = LauncherEntryOrder.BuildIndex(customOrder);
        return entries
            .OrderBy(entry => order.TryGetValue(entry.Id, out var index) ? index : int.MaxValue)
            .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal);
    }

    private static DateTimeOffset ParseUpdatedAt(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }
}
