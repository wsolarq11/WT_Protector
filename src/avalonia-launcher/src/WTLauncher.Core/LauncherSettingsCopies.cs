namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    public static LauncherSettings Clone(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new LauncherSettings
        {
            LastFilterText = settings.LastFilterText,
            SortMode = settings.SortMode,
            EntryViewMode = settings.EntryViewMode,
            RequireConfirmationForLaunches = settings.RequireConfirmationForLaunches,
            CustomEntryOrder = CloneCustomEntryOrder(settings)!,
            Window = CloneWindow(settings.Window)!,
        };
    }

    public static void CopyTo(LauncherSettings source, LauncherSettings target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        target.LastFilterText = source.LastFilterText;
        target.SortMode = source.SortMode;
        target.EntryViewMode = source.EntryViewMode;
        target.RequireConfirmationForLaunches = source.RequireConfirmationForLaunches;
        target.CustomEntryOrder = CloneCustomEntryOrder(source)!;
        target.Window = CloneWindow(source.Window)!;
    }

    public static List<string>? CloneCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.CustomEntryOrder is null ? null : [.. settings.CustomEntryOrder];
    }

    private static LauncherWindowSettings? CloneWindow(LauncherWindowSettings? window) =>
        window is null
            ? null
            : new LauncherWindowSettings
            {
                Width = window.Width,
                Height = window.Height,
            };
}
