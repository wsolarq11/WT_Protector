namespace WTLauncher.Core;

public sealed class LauncherSettingsSnapshot
{
    private LauncherSettingsSnapshot(
        string theme,
        string lastFilterText,
        string sortMode,
        string entryViewMode,
        bool requireConfirmationForLaunches,
        bool hasCustomEntryOrder,
        List<string> customEntryOrder,
        bool hasWindow,
        double windowWidth,
        double windowHeight)
    {
        Theme = theme;
        LastFilterText = lastFilterText;
        SortMode = sortMode;
        EntryViewMode = entryViewMode;
        RequireConfirmationForLaunches = requireConfirmationForLaunches;
        HasCustomEntryOrder = hasCustomEntryOrder;
        CustomEntryOrder = customEntryOrder;
        HasWindow = hasWindow;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
    }

    private string Theme { get; }

    public string LastFilterText { get; }

    public string SortMode { get; }

    public string EntryViewMode { get; }

    public bool RequireConfirmationForLaunches { get; }

    private bool HasCustomEntryOrder { get; }

    private IReadOnlyList<string> CustomEntryOrder { get; }

    private bool HasWindow { get; }

    private double WindowWidth { get; }

    private double WindowHeight { get; }

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
            settings.CustomEntryOrder is not null,
            settings.CustomEntryOrder is null ? [] : [.. settings.CustomEntryOrder],
            settings.Window is not null,
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
        settings.CustomEntryOrder = HasCustomEntryOrder ? CloneCustomEntryOrder() : null!;
        settings.Window = HasWindow
            ? new LauncherWindowSettings
            {
                Width = WindowWidth,
                Height = WindowHeight,
            }
            : null!;
    }

    public void RestoreCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.CustomEntryOrder = HasCustomEntryOrder ? CloneCustomEntryOrder() : null!;
    }

    public List<string> CloneCustomEntryOrder()
    {
        return [.. CustomEntryOrder];
    }
}
