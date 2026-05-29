namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    public static LauncherSettings Load(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        if (!File.Exists(settingsPath))
            return new LauncherSettings();

        return LauncherJson.Load<LauncherSettings>(settingsPath) ?? new LauncherSettings();
    }

    public static void Save(LauncherSettings settings, string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));

        LauncherJson.Save(settings, settingsPath);
    }

    public static bool TrySaveWindowBounds(LauncherSettings settings, string settingsPath, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));

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
}
