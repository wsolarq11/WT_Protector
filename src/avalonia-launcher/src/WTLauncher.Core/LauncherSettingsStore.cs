namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    public static LauncherSettings Load(string settingsPath)
    {
        ThrowIfSettingsPathMissing(settingsPath);
        if (!File.Exists(settingsPath))
            return new LauncherSettings();

        var settings = LauncherJson.Load<LauncherSettings>(settingsPath) ?? new LauncherSettings();
        Normalize(settings);
        return settings;
    }

    public static void Save(LauncherSettings settings, string settingsPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        Normalize(settings);
        LauncherJson.Save(settings, settingsPath);
    }

    public static bool TrySaveWindowBounds(LauncherSettings settings, string settingsPath, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        var window = settings.Window ??= new LauncherWindowSettings();
        var previousWidth = window.Width;
        var previousHeight = window.Height;
        window.Width = width;
        window.Height = height;

        try
        {
            Save(settings, settingsPath);
            return true;
        }
        // Close-time bounds persistence must not leave unsaved sizes in memory.
        // Write failures can surface as IOException, UnauthorizedAccessException,
        // or other path/storage exceptions depending on the underlying cause.
        catch (Exception)
        {
            window.Width = previousWidth;
            window.Height = previousHeight;
            return false;
        }
    }

    private static void ThrowIfSettingsPathMissing(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
    }

    private static void Normalize(LauncherSettings settings)
    {
        settings.Theme ??= "light";
        settings.LastFilterText ??= string.Empty;
        settings.SortMode = LauncherEntryVisibility.NormalizeSortModeId(settings.SortMode);
        settings.EntryViewMode = LauncherEntryViewModes.NormalizeId(settings.EntryViewMode);
        settings.CustomEntryOrder = LauncherEntryIds.ToUniqueUsableList(settings.CustomEntryOrder ?? []);
        settings.Window ??= new LauncherWindowSettings();
    }
}
