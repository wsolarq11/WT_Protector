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
        return new LauncherSettingsSnapshot(settings with { SchemaVersion = 1 });
    }

    public LauncherSettings Restore()
    {
        return _settings with { SchemaVersion = 1 };
    }

    public LauncherSettings RestoreCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            // ImmutableList has value semantics; no need to copy.
            CustomEntryOrder = _settings.CustomEntryOrder,
        };
    }

}
