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

        settings.Window ??= new LauncherWindowSettings();
        var previousWidth = settings.Window.Width;
        var previousHeight = settings.Window.Height;
        settings.Window.Width = width;
        settings.Window.Height = height;

        try
        {
            Save(settings, settingsPath);
            return true;
        }
        catch
        {
            settings.Window.Width = previousWidth;
            settings.Window.Height = previousHeight;
            return false;
        }
    }
}
