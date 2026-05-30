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

        var presets = LauncherJson.Load<LauncherPresetFile>(presetPath)
            ?? throw new InvalidDataException("Launcher presets parsed to null.");
        Normalize(presets);
        return presets;
    }

    public static void Save(LauncherPresetFile presets, string presetPath, bool backup = true)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));

        LauncherJson.Save(presets, presetPath, backup);
    }

    public static string ResolvePresetPath(string configPath)
    {
        return LauncherConfigStore.ResolveSiblingPath(configPath, PresetsFileName);
    }

    private static void Normalize(LauncherPresetFile presets)
    {
        presets.DefaultPresetId ??= string.Empty;
        presets.Presets = (presets.Presets ?? [])
            .OfType<LauncherPreset>()
            .ToList();

        foreach (var preset in presets.Presets)
        {
            preset.Id ??= string.Empty;
            preset.Name ??= string.Empty;
            preset.EntryIds = (preset.EntryIds ?? [])
                .Where(id => id is not null)
                .ToList();
        }
    }
}
