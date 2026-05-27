using System.Collections.ObjectModel;

namespace WTLauncher.Core;

public static class LauncherVisibleEntryCollection
{
    public static int Replace<T>(
        ObservableCollection<T> target,
        IEnumerable<T> entries,
        string filterText,
        string? sortModeId,
        List<string> customEntryOrder)
        where T : IHasLaunchEntry
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var source = entries as IReadOnlyList<T> ?? entries.ToList();
        target.Clear();

        foreach (var entry in LauncherEntrySorter.Sort(
                     source.Where(entry => LauncherEntrySearch.Matches(entry.Entry, filterText)),
                     sortModeId,
                     customEntryOrder,
                     source.Select(entry => entry.Entry.Id)))
        {
            target.Add(entry);
        }

        return target.Count;
    }
}
