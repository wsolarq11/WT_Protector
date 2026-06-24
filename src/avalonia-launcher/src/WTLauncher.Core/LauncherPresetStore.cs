namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";
    public const int ExpectedPresetSchemaVersion = 1;

    public static LauncherPresetFile Load(string presetPath)
    {
        ThrowIfPresetPathMissing(presetPath);
        if (!File.Exists(presetPath))
        {
            var backupPath = presetPath + ".bak";
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, presetPath);
            }
            else
            {
                return new LauncherPresetFile();
            }
        }

        var presets = LauncherJson.LoadRequired<LauncherPresetFile>(presetPath, "Launcher presets");
        if (presets.SchemaVersion != ExpectedPresetSchemaVersion)
            throw new InvalidDataException($"Launcher presets schema version mismatch: expected {ExpectedPresetSchemaVersion}, got {presets.SchemaVersion}.");
        return LauncherPresetFiles.Validate(presets);
    }

    public static void Save(LauncherPresetFile presets, string presetPath, bool backup)
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
