using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsWorkflow
{
    public static LauncherSettingsMutationResult ChangeFilterText(
        LauncherSettings settings,
        string settingsPath,
        string? filterText,
        Action<LauncherSettingsSnapshot> onRollback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsPath);
        ArgumentNullException.ThrowIfNull(onRollback);

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.LastFilterText = filterText ?? string.Empty;
        return Save(settings, settingsPath, snapshot, onRollback);
    }

    public static LauncherSettingsMutationResult ChangeSortMode(
        LauncherSettings settings,
        string settingsPath,
        string sortModeId,
        Action ensureCustomOrderContainsEntries,
        Action<LauncherSettingsSnapshot> onRollback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsPath);
        ArgumentNullException.ThrowIfNull(sortModeId);
        ArgumentNullException.ThrowIfNull(ensureCustomOrderContainsEntries);
        ArgumentNullException.ThrowIfNull(onRollback);

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.SortMode = sortModeId;
        ensureCustomOrderContainsEntries();
        return Save(settings, settingsPath, snapshot, onRollback);
    }

    public static LauncherSettingsMutationResult ChangeDisplayMode(
        LauncherSettings settings,
        string settingsPath,
        string displayModeId,
        Action afterMutation,
        Action<LauncherSettingsSnapshot> onRollback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsPath);
        ArgumentNullException.ThrowIfNull(displayModeId);
        ArgumentNullException.ThrowIfNull(afterMutation);
        ArgumentNullException.ThrowIfNull(onRollback);

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.EntryViewMode = displayModeId;
        afterMutation();
        return Save(settings, settingsPath, snapshot, onRollback);
    }

    public static LauncherSettingsMutationResult ChangeLaunchConfirmation(
        LauncherSettings settings,
        string settingsPath,
        bool requireConfirmationForLaunches,
        Action afterMutation,
        Action<LauncherSettingsSnapshot> onRollback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsPath);
        ArgumentNullException.ThrowIfNull(afterMutation);
        ArgumentNullException.ThrowIfNull(onRollback);

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.RequireConfirmationForLaunches = requireConfirmationForLaunches;
        afterMutation();
        return Save(settings, settingsPath, snapshot, onRollback);
    }

    private static LauncherSettingsMutationResult Save(
        LauncherSettings settings,
        string settingsPath,
        LauncherSettingsSnapshot snapshot,
        Action<LauncherSettingsSnapshot> onRollback)
    {
        return LauncherSettingsMutationPersistence.Save(
            settings,
            settingsPath,
            snapshot,
            () => onRollback(snapshot));
    }
}
