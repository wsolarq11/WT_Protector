using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryReload
{
    public static LauncherEntryCollectionSummary Load(
        string configPath,
        ObservableCollection<LauncherEntryViewModel> entries,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(createViewModel);

        return LauncherEntryCollection.Replace(
            entries,
            LauncherConfigStore.Load(configPath),
            createViewModel);
    }
}
