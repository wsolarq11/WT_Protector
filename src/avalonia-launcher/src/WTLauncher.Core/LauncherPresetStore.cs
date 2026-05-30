namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";

    public static LauncherPresetFile Load(string presetPath)
    {
        ThrowIfPresetPathMissing(presetPath);
        if (!File.Exists(presetPath))
            return new LauncherPresetFile();

        var presets = LauncherJson.Load<LauncherPresetFile>(presetPath)
            ?? throw new InvalidDataException("Launcher presets parsed to null.");
        LauncherPresetFiles.Normalize(presets);
        return presets;
    }

    public static void Save(LauncherPresetFile presets, string presetPath, bool backup = true)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ThrowIfPresetPathMissing(presetPath);

        LauncherJson.Save(LauncherPresetFiles.Clone(presets), presetPath, backup);
    }

    public static string ResolvePresetPath(string configPath)
    {
        return LauncherConfigStore.ResolveSiblingPath(configPath, PresetsFileName);
    }

    private static void ThrowIfPresetPathMissing(string? presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));
    }
}
