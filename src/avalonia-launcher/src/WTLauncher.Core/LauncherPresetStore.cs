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

            // Schema migration contract:
            // - SchemaVersion indicates the format version of this file.
            // - OLDER versions (schema < expected): rejected — the app cannot read them.
            // - EXACT version: loaded normally.
            // - NEWER versions (schema > expected): rejected with an upgrade message.
            //   This prevents data loss from downgrading after a schema change.
            // - When bumping ExpectedSchemaVersion, add a migration path that reads
            //   the old version and transforms it to the new format before validation.
            if (presets.SchemaVersion < ExpectedPresetSchemaVersion)
                throw new InvalidDataException($"Launcher presets schema version {presets.SchemaVersion} is too old (expected at least {ExpectedPresetSchemaVersion}).");
            if (presets.SchemaVersion > ExpectedPresetSchemaVersion)
                throw new InvalidDataException($"Launcher presets schema version {presets.SchemaVersion} is from a newer version of the app. Please upgrade WT Launcher.");
            return LauncherPresetFiles.Validate(presets);
        }

        if (File.Exists(presetPath))
        {
            Exception? loadException = null;
            try
            {
                return LoadAndValidate(presetPath);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException)
            {
                loadException = ex;
            }

            if (loadException is not null)
            {
                // Main file is corrupt — try .bak recovery.
                var backupPath = presetPath + ".bak";
                if (File.Exists(backupPath))
                {
                    var recoveryTempPath = presetPath + $".recovery-{Guid.NewGuid():N}";
                    File.Copy(backupPath, recoveryTempPath, overwrite: true);
                    File.Move(recoveryTempPath, presetPath, overwrite: true);
                    // Retry outside try/catch — if backup is also corrupt, the exception
                    // propagates naturally without looping.
                    return LoadAndValidate(presetPath);
                }
                throw loadException; // No backup available — propagate original exception.
            }
        }

        // File missing — try backup or return default.
        var backupPathForMissing = presetPath + ".bak";
        if (File.Exists(backupPathForMissing))
        {
            var recoveryTempPath = presetPath + $".recovery-{Guid.NewGuid():N}";
            File.Copy(backupPathForMissing, recoveryTempPath, overwrite: true);
            File.Move(recoveryTempPath, presetPath, overwrite: true);
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
