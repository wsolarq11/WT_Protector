namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    public static LauncherSettings Clone(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new LauncherSettings
        {
            SchemaVersion = settings.SchemaVersion,
            LastFilterText = settings.LastFilterText,
            SortMode = settings.SortMode,
            EntryViewMode = settings.EntryViewMode,
            RequireConfirmationForLaunches = settings.RequireConfirmationForLaunches,
            CommandTemplates = CloneCommandTemplates(settings.CommandTemplates),
            CustomEntryOrder = CloneCustomEntryOrder(settings)!,
            SearchHistory = [.. settings.SearchHistory],
            Window = CloneWindow(settings.Window)!,
        };
    }

    public static void CopyTo(LauncherSettings source, LauncherSettings target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        target.SchemaVersion = source.SchemaVersion;
        target.LastFilterText = source.LastFilterText;
        target.SortMode = source.SortMode;
        target.EntryViewMode = source.EntryViewMode;
        target.RequireConfirmationForLaunches = source.RequireConfirmationForLaunches;
        target.CommandTemplates = CloneCommandTemplates(source.CommandTemplates);
        target.CustomEntryOrder = CloneCustomEntryOrder(source)!;
        target.SearchHistory = [.. source.SearchHistory];
        target.Window = CloneWindow(source.Window)!;
    }

    public static List<string>? CloneCustomEntryOrder(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.CustomEntryOrder is null ? null : [.. settings.CustomEntryOrder];
    }

    public static Dictionary<string, string> CloneCommandTemplates(Dictionary<string, string> templates) =>
        new(templates, StringComparer.OrdinalIgnoreCase);

    private static LauncherWindowSettings? CloneWindow(LauncherWindowSettings? window) =>
        window is null
            ? null
            : new LauncherWindowSettings
            {
                Width = window.Width,
                Height = window.Height,
            };
}
