using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherVisibleEntryCollection
{
    public static int Replace(
        ObservableCollection<LauncherEntryViewModel> target,
        IEnumerable<LauncherEntryViewModel> entries,
        string filterText,
        string? sortModeId,
        List<string> customEntryOrder)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var source = entries as IReadOnlyList<LauncherEntryViewModel> ?? entries.ToList();
        target.Clear();

        foreach (var entry in LauncherEntrySorter.Sort(
                     source.Where(entry => LauncherEntrySearch.Matches(entry.Entry, filterText)),
                     sortModeId,
                     customEntryOrder,
                     source.Select(entry => entry.Id)))
        {
            target.Add(entry);
        }

        return target.Count;
    }
}
