using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryCollectionSaveWorkflow
{
    public static LauncherEntryCollectionSaveResult Save(
        IReadOnlyCollection<LauncherEntryViewModel> entries,
        string configPath,
        LauncherPresetFile presetFile,
        string presetPath,
        LauncherSettings settings,
        string settingsPath,
        bool savePresets,
        LauncherEntryMutationSnapshot? rollbackSnapshot,
        Action beforeSaveSettings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(configPath);
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presetPath);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsPath);
        ArgumentNullException.ThrowIfNull(beforeSaveSettings);

        var totalCount = entries.Count;
        var selectedCount = entries.Count(entry => entry.IsSelected);
        var result = LauncherEntryMutationPersistence.Save(
            entries.Select(entry => entry.Entry),
            configPath,
            presetFile,
            presetPath,
            settings,
            settingsPath,
            savePresets,
            rollbackSnapshot,
            beforeSaveSettings);

        return result.Succeeded
            ? LauncherEntryCollectionSaveResult.Saved(totalCount, selectedCount)
            : LauncherEntryCollectionSaveResult.Failed(totalCount, selectedCount, result.StatusText);
    }
}

public readonly record struct LauncherEntryCollectionSaveResult(
    bool Succeeded,
    int TotalCount,
    int SelectedCount,
    string? StatusText)
{
    public static LauncherEntryCollectionSaveResult Saved(int totalCount, int selectedCount)
    {
        return new LauncherEntryCollectionSaveResult(true, totalCount, selectedCount, null);
    }

    public static LauncherEntryCollectionSaveResult Failed(int totalCount, int selectedCount, string? statusText)
    {
        return new LauncherEntryCollectionSaveResult(false, totalCount, selectedCount, statusText);
    }
}
