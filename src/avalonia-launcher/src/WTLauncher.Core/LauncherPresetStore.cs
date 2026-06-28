
namespace WTLauncher.Core;

public static class LauncherPresetStore
{
    public const string PresetsFileName = "presets.json";
    public const int ExpectedPresetSchemaVersion = 2;

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
            // Schema v1: migrate to v2 equivalently — no structural changes.
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

        LauncherJson.Save(LauncherPresetFiles.Validate(presets), presetPath, backup);
    }

    // ponytail: ResolvePresetPath removed — presets now live in the unified
    // LauncherStoreFile alongside entries and settings. Use LauncherStore
    // for all path resolution and load/save operations.

    private static void ThrowIfPresetPathMissing(string? presetPath)
    {
        if (string.IsNullOrWhiteSpace(presetPath))
            throw new ArgumentException("Preset path is required.", nameof(presetPath));
    }
}
