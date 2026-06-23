namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    /// <summary>Deep-clones settings. Mutable collections (CommandTemplates, CustomEntryOrder,
    /// SearchHistory) are copied; Window is a value-type record (cloned via `with { }`).</summary>
    public static LauncherSettings Clone(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            CommandTemplates = CloneCommandTemplates(settings.CommandTemplates),
            // Null-preserving: CustomEntryOrder can be null in edge cases (old-format settings,
            // snapshot restore). [JsonRequired] prevents null from JSON, not from in-memory construction.
            CustomEntryOrder = settings.CustomEntryOrder is null ? null! : [.. settings.CustomEntryOrder],
            SearchHistory = [.. settings.SearchHistory],
            // Null-preserving: Window can be null in edge cases (see CustomEntryOrder comment).
            Window = settings.Window is null ? null! : settings.Window with { },
        };
    }

    public static Dictionary<string, string> CloneCommandTemplates(Dictionary<string, string> templates) =>
        new(templates, StringComparer.OrdinalIgnoreCase);
}
