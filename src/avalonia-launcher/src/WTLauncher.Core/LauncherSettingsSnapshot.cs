namespace WTLauncher.Core;

public sealed class LauncherSettingsSnapshot
{
    private LauncherSettingsSnapshot(
        string theme,
        string lastFilterText,
        string sortMode,
        string entryViewMode,
        bool requireConfirmationForLaunches,
        List<string> customEntryOrder,
        double windowWidth,
        double windowHeight)
    {
        Theme = theme;
        LastFilterText = lastFilterText;
        SortMode = sortMode;
        EntryViewMode = entryViewMode;
        RequireConfirmationForLaunches = requireConfirmationForLaunches;
        CustomEntryOrder = customEntryOrder;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
    }

    public string Theme { get; }

    public string LastFilterText { get; }

    public string SortMode { get; }

    public string EntryViewMode { get; }

    public bool RequireConfirmationForLaunches { get; }

    public IReadOnlyList<string> CustomEntryOrder { get; }

    public double WindowWidth { get; }

    public double WindowHeight { get; }

    public static LauncherSettingsSnapshot Capture(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var window = settings.Window ?? new LauncherWindowSettings();
        return new LauncherSettingsSnapshot(
            settings.Theme,
            settings.LastFilterText,
            settings.SortMode,
            settings.EntryViewMode,
            settings.RequireConfirmationForLaunches,
            [.. settings.CustomEntryOrder],
            window.Width,
            window.Height);
    }

    public void Restore(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Theme = Theme;
        settings.LastFilterText = LastFilterText;
        settings.SortMode = SortMode;
        settings.EntryViewMode = EntryViewMode;
        settings.RequireConfirmationForLaunches = RequireConfirmationForLaunches;
        settings.CustomEntryOrder = CloneCustomEntryOrder();
        settings.Window = new LauncherWindowSettings
        {
            Width = WindowWidth,
            Height = WindowHeight,
        };
    }

    public void RestoreCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.CustomEntryOrder = CloneCustomEntryOrder();
    }

    public List<string> CloneCustomEntryOrder()
    {
        return [.. CustomEntryOrder];
    }
}
