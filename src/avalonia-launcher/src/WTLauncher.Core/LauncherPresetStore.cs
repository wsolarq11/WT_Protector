
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

        return LauncherJson.LoadWithRecovery(presetPath, LoadAndValidate, "PresetLoad.RecoveredFromBackup", LauncherPresetFiles.Validate(new LauncherPresetFile()));
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
