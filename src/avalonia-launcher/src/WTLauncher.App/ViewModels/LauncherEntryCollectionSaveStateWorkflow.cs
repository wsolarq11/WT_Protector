using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryCollectionSaveStateWorkflow
{
    public static bool Save(
        IReadOnlyCollection<LauncherEntryViewModel> entries,
        string configPath,
        LauncherPresetFile presetFile,
        string presetPath,
        LauncherSettings settings,
        string settingsPath,
        bool savePresets,
        LauncherEntryMutationSnapshot? rollbackSnapshot,
        Action beforeSaveSettings,
        Action<LauncherEntryCollectionSaveState> applyState,
        Action<string> updateStatus,
        Action afterSave)
    {
        ArgumentNullException.ThrowIfNull(applyState);
        ArgumentNullException.ThrowIfNull(updateStatus);
        ArgumentNullException.ThrowIfNull(afterSave);

        var result = LauncherEntryCollectionSaveWorkflow.Save(
            entries,
            configPath,
            presetFile,
            presetPath,
            settings,
            settingsPath,
            savePresets,
            rollbackSnapshot,
            beforeSaveSettings);
        applyState(new LauncherEntryCollectionSaveState(result.TotalCount, result.SelectedCount));

        if (!result.Succeeded)
        {
            if (result.StatusText is not null)
                updateStatus(result.StatusText);
            return false;
        }

        afterSave();
        return true;
    }
}

public sealed record LauncherEntryCollectionSaveState(int TotalCount, int SelectedCount);
