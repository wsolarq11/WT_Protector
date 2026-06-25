using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";
    public const int ExpectedPresetSchemaVersion = 1;

    public static LauncherPresetFile Load(string presetPath)
    {
        ThrowIfPresetPathMissing(presetPath);

        LauncherPresetFile LoadAndValidate(string path)
        {
            var presets = LauncherJson.LoadRequired<LauncherPresetFile>(path, "Launcher presets");
            if (presets.SchemaVersion != ExpectedPresetSchemaVersion)
                throw new InvalidDataException($"Launcher presets schema version mismatch: expected {ExpectedPresetSchemaVersion}, got {presets.SchemaVersion}.");
            return LauncherPresetFiles.Validate(presets);
        }

        if (File.Exists(presetPath))
        {
            try
            {
                return LoadAndValidate(presetPath);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                // Main file is corrupt — try .bak recovery.
                var backupPath = presetPath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, presetPath, overwrite: true);
                    return LoadAndValidate(presetPath);
                }
                throw; // No backup available — propagate original exception.
            }
        }

        // File missing — try backup or return default.
        var backupPathForMissing = presetPath + ".bak";
        if (File.Exists(backupPathForMissing))
        {
            File.Copy(backupPathForMissing, presetPath);
            return LoadAndValidate(presetPath);
        }

        return new LauncherPresetFile();
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
