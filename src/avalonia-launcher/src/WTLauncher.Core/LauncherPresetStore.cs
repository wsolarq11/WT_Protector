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
        ArgumentNullException.ThrowIfNull(presets);
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));

        Normalize(presets);
        LauncherJson.Save(presets, presetPath, backup);
    }

    public static string ResolvePresetPath(string configPath)
    {
        return LauncherConfigStore.ResolveSiblingPath(configPath, PresetsFileName);
    }

    private static void Normalize(LauncherPresetFile presets)
    {
        var normalizedPresets = new List<LauncherPreset>();
        var usedPresetIds = new List<string>();
        foreach (var preset in (presets.Presets ?? []).OfType<LauncherPreset>())
        {
            preset.Id = LauncherPresetIds.GenerateUnique(preset.Id, usedPresetIds);
            usedPresetIds.Add(preset.Id);
            preset.Name ??= string.Empty;
            preset.EntryIds = LauncherEntryIds.ToUniqueUsableList(preset.EntryIds ?? []);
            normalizedPresets.Add(preset);
        }

        presets.Presets = normalizedPresets;
        presets.DefaultPresetId = NormalizePresetId(presets.DefaultPresetId);
        if (normalizedPresets.Count == 0)
        {
            presets.DefaultPresetId = string.Empty;
            return;
        }

        if (!normalizedPresets.Any(preset => LauncherPresetIds.Same(preset.Id, presets.DefaultPresetId)))
            presets.DefaultPresetId = normalizedPresets[0].Id;
    }

    private static string NormalizePresetId(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : LauncherPresetIds.Normalize(value);
}
