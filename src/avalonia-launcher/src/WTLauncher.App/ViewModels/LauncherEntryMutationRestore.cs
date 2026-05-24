using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryMutationRestore
{
    public static LauncherEntryCollectionSummary Restore(
        LauncherEntryMutationSnapshot snapshot,
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherSettings settings,
        Action<LauncherPresetFile, string?> restorePresetFile,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(restorePresetFile);
        ArgumentNullException.ThrowIfNull(createViewModel);

        var summary = LauncherEntryCollection.Replace(
            entries,
            snapshot.CloneEntries(),
            createViewModel);

        settings.CustomEntryOrder = snapshot.CloneCustomEntryOrder();
        restorePresetFile(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        return summary;
    }
}
