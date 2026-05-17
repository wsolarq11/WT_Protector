namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";

    public static LauncherPresetFile Load(string presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));
        if (!File.Exists(presetPath))
            return new LauncherPresetFile();

        return LauncherJson.Load<LauncherPresetFile>(presetPath)
            ?? throw new InvalidDataException("Launcher presets parsed to null.");
    }

    public static void Save(LauncherPresetFile presets, string presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));

        LauncherJson.Save(presets, presetPath, backup: true);
    }

    public static string ResolvePresetPath(string configPath)
    {
        return LauncherConfigStore.ResolveSiblingPath(configPath, PresetsFileName);
    }
}
