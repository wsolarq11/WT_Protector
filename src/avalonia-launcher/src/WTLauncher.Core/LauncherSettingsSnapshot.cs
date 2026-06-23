namespace WTLauncher.Core;

public sealed class LauncherSettingsSnapshot
{
    private readonly LauncherSettings _settings;

    private LauncherSettingsSnapshot(LauncherSettings settings)
    {
        _settings = settings;
    }

    public string LastFilterText => _settings.LastFilterText;

    public string SortMode => _settings.SortMode;

    public string EntryViewMode => _settings.EntryViewMode;

    public bool RequireConfirmationForLaunches => _settings.RequireConfirmationForLaunches;

    public static LauncherSettingsSnapshot Capture(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new LauncherSettingsSnapshot(LauncherSettingsCopies.Clone(settings));
    }

    public LauncherSettings Restore(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return LauncherSettingsCopies.Clone(_settings);
    }

    public LauncherSettings RestoreCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            CustomEntryOrder = _settings.CustomEntryOrder is null ? null! : [.. _settings.CustomEntryOrder],
        };
    }

}
