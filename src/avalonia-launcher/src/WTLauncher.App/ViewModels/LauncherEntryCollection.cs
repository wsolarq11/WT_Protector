using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryCollection
{
    public static LauncherEntryCollectionSummary Replace(
        ObservableCollection<LauncherEntryViewModel> target,
        IEnumerable<LaunchEntry> entries,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(createViewModel);

        target.Clear();
        foreach (var entry in entries)
        {
            target.Add(createViewModel(entry));
        }

        return new LauncherEntryCollectionSummary(
            target.Count,
            target.Count(item => item.IsSelected));
    }
}

public sealed record LauncherEntryCollectionSummary(int TotalCount, int SelectedCount);
