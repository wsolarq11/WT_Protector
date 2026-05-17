using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static LauncherSettings Load(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        if (!File.Exists(settingsPath))
            return new LauncherSettings();

        using var stream = File.OpenRead(settingsPath);
        return JsonSerializer.Deserialize<LauncherSettings>(stream, Options) ?? new LauncherSettings();
    }

    public static void Save(LauncherSettings settings, string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(settingsPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(settingsPath);
        JsonSerializer.Serialize(stream, settings, Options);
    }
}
