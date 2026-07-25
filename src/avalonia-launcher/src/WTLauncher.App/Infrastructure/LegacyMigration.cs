using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Zone 1: One-time migration from legacy three-file config (config.json +
/// presets.json + settings.json) to the unified launcher.json format.
/// </summary>
internal static class LegacyMigration
{
    private static readonly FileLauncherJsonIO _io = new();

    /// <summary>Detects old config.json/presets.json/settings.json and migrates to launcher.json.</summary>
    public static LauncherStoreFile? MigrateFromLegacyFiles(string configDirectory)
    {
        var configPath = Path.Combine(configDirectory, "config.json");
        var presetsPath = Path.Combine(configDirectory, "presets.json");
        var settingsPath = Path.Combine(configDirectory, "settings.json");
        var storePath = Path.Combine(configDirectory, LauncherStore.StoreFileName);

        if (File.Exists(storePath))
            return null;
        if (!File.Exists(configPath))
            return null;

        var configItems = _io.Load<List<LaunchEntry>>(configPath)
            ?? throw new InvalidDataException("Failed to load legacy config.json");

        var settings = new LauncherSettings();
        if (File.Exists(settingsPath))
        {
            try { settings = _io.LoadRequired<LauncherSettings>(settingsPath, "legacy settings"); }
            catch (Exception ex) when (ex is JsonException or IOException or FileNotFoundException)
            {
                Trace.WriteLine($"[LauncherStore] Legacy settings.json unreadable, using defaults: {ex.Message}");
            }
        }

        ImmutableList<LauncherPreset>? presets = null;
        var defaultPresetId = "";
        if (File.Exists(presetsPath))
        {
            try
            {
                var legacy = _io.Load<WTLauncher.Core.LegacyPresetDocument>(presetsPath);
                if (legacy is not null)
                {
                    presets = legacy.Presets;
                    defaultPresetId = legacy.DefaultPresetId ?? "";
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or FileNotFoundException)
            {
                Trace.WriteLine($"[LauncherStore] Legacy presets.json unreadable, using empty presets: {ex.Message}");
            }
        }

        var storeFile = new LauncherStoreFile
        {
            Entries = (configItems ?? []).ToImmutableList(),
            Settings = settings,
            Presets = presets ?? ImmutableList<LauncherPreset>.Empty,
            DefaultPresetId = defaultPresetId,
        };

        _io.Save(storeFile, storePath, backup: true);

        TryMoveToBak(configPath);
        TryMoveToBak(presetsPath);
        TryMoveToBak(settingsPath);

        return storeFile;
    }

    private static void TryMoveToBak(string path)
    {
        try { File.Move(path, path + ".bak", overwrite: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            Trace.WriteLine($"[LauncherStore] Legacy file rename to .bak failed for {path}: {ex.Message}");
        }
    }
}
