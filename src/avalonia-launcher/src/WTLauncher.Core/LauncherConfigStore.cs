using System.Collections.Immutable;
using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherConfigStore
{
    public const string ConfigDirectoryName = "config";
    public const string ConfigFileName = "config.json";
    public const string SettingsFileName = "settings.json";
    public const string RepositoryMarkerFileName = "README.md";
    public const int ExpectedConfigSchemaVersion = 1;

    /// <summary>
    /// Schema migration contract:
    /// - SchemaVersion indicates the format version of this file.
    /// - Version 0 (legacy raw JSON array with no schemaVersion): accepted as-is,
    ///   migrated to version 1 on next save.
    /// - Versions 1..ExpectedConfigSchemaVersion: loaded normally.
    /// - Versions > ExpectedConfigSchemaVersion: rejected with an upgrade message.
    ///   This prevents data loss from downgrading after a schema change.
    ///
    /// Migration pattern for future version bumps (e.g. bumping to version 2):
    ///   if (configFile.SchemaVersion == 1)
    ///   {
    ///       // Transform v1 entries to v2 format here.
    ///       // Example: add a new required field with a default value.
    ///       configFile = configFile with { Entries = migratedEntries, SchemaVersion = 2 };
    ///   }
    ///   else if (configFile.SchemaVersion == 2)
    ///   {
    ///       // Already at v2 — no migration needed.
    ///   }
    /// Then bump ExpectedConfigSchemaVersion to 2.
    /// </summary>

    public static IReadOnlyList<LaunchEntry> Load(string configPath)
    {
        ThrowIfConfigPathMissing(configPath);

        // Single-pass load for the new wrapper format; two-pass only for legacy raw
        // array fallback. Legacy files are migrated to wrapper format on next save.
        IReadOnlyList<LaunchEntry> LoadAndValidate(string path)
        {
            var fileInfo = new FileInfo(path);
            // Reject unreasonably large config files before deserialization to prevent
            // out-of-memory attacks. 50 MB is far beyond any legitimate launcher config.
            if (fileInfo.Length > 50 * 1024 * 1024)
                throw new InvalidDataException("Launcher config file exceeds 50 MB size limit.");

            using var stream = File.OpenRead(path);

            // Quick format detection: read the first non-whitespace byte.
            // A JSON array means legacy format (no schemaVersion wrapper).
            // Everything else (object, null, or garbage) goes through the
            // wrapper path where errors propagate naturally.
            // ponytail: single-byte peek, not JsonDocument.Parse — avoids
            // the double-memory-footprint issue the original single-pass
            // design eliminated.
            var isLegacyArray = false;
            {
                int b;
                while ((b = stream.ReadByte()) != -1)
                {
                    if (b is not ((int)' ' or (int)'\t' or (int)'\n' or (int)'\r'))
                    {
                        isLegacyArray = b == (int)'[';
                        break;
                    }
                }
                stream.Position = 0;
            }

            if (isLegacyArray)
            {
                // Legacy format: raw JSON array of LaunchEntry objects.
                var legacyEntries = JsonSerializer.Deserialize<List<LaunchEntry?>>(stream, LauncherJson.Options)
                    ?? throw new InvalidDataException("Launcher config parsed to null.");

                return NormalizeEntries(legacyEntries).AsReadOnly();
            }

            // Current wrapper format (or corrupt — let exceptions propagate).
            var configFile = JsonSerializer.Deserialize<LauncherConfigFile>(stream, LauncherJson.Options)
                ?? throw new InvalidDataException("Launcher config parsed to null.");

            // Validate schema version: reject files from newer app versions.
            // Accept schemaVersion <= expected (forward compat — older files
            // are readable; the app normalizes on load).
            if (configFile.SchemaVersion > ExpectedConfigSchemaVersion)
                throw new InvalidDataException(
                    $"Launcher config schema version {configFile.SchemaVersion} is from a newer version of the app. Please upgrade WT Launcher.");

            var entries = configFile.Entries
                ?? throw new InvalidDataException("Launcher config entries list is null.");

            return NormalizeEntries(entries).AsReadOnly();
        }

        return LauncherJson.LoadWithRecovery(configPath, LoadAndValidate, "ConfigLoad.RecoveredFromBackup", Array.Empty<LaunchEntry>());
    }

    public static void Save(IEnumerable<LaunchEntry> entries, string configPath, bool backup)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ThrowIfConfigPathMissing(configPath);

        var normalized = NormalizeEntries(entries!);

        LauncherJson.Save(
            new LauncherConfigFile
            {
                SchemaVersion = ExpectedConfigSchemaVersion,
                Entries = [.. normalized],
            },
            configPath,
            backup);
    }

    // Path.GetFullPath follows NTFS symlinks and junction points by design —
    // it always resolves to the final, real path. This is the correct behavior
    // for config resolution: we want canonical paths, not symlink aliases.
    public static string ResolveConfigPath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null)
    {
        if (explicitPath is not null)
        {
            var trimmedExplicitPath = explicitPath.Trim();
            if (trimmedExplicitPath.Length == 0)
                throw new ArgumentException("Explicit config path is required.", nameof(explicitPath));

            var fullPath = Path.GetFullPath(trimmedExplicitPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Config file not found: {fullPath}", fullPath);

            var fileName = Path.GetFileName(fullPath);
            var dirName = Path.GetFileName(Path.GetDirectoryName(fullPath));
            if (!string.Equals(fileName, ConfigFileName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(dirName, ConfigDirectoryName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Explicit config path must end with {ConfigDirectoryName}/{ConfigFileName}, got: {fullPath}",
                    nameof(explicitPath));

            return fullPath;
        }

        string?[] candidateRoots = [currentDirectory, appBaseDirectory];
        foreach (var candidateRoot in candidateRoots)
        {
            var found = FindUpward(candidateRoot);
            if (found is not null)
                return found;
        }

        foreach (var candidateRoot in candidateRoots)
        {
            if (string.IsNullOrWhiteSpace(candidateRoot))
                continue;

            var config = BuildConfigPath(candidateRoot);
            if (File.Exists(config))
                return config;
        }

        return BuildConfigPath(RequireSearchRoot(candidateRoots));
    }

    public static string ResolveSettingsPath(string configPath)
    {
        return ResolveSiblingPath(configPath, SettingsFileName);
    }

    internal static string ResolveSiblingPath(string configPath, string fileName)
    {
        ThrowIfConfigPathMissing(configPath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath.Trim()))
            ?? throw new InvalidOperationException("Config path must have a parent directory.");
        return Path.Combine(directory, fileName);
    }

    private static void ThrowIfConfigPathMissing(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));
    }

    private static string? FindUpward(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            var config = BuildConfigPath(dir.FullName);
            if (File.Exists(config) && File.Exists(BuildRepositoryMarkerPath(dir.FullName)))
                return config;
        }

        return null;
    }

    private static string RequireSearchRoot(IEnumerable<string?> candidateRoots)
    {
        foreach (var candidateRoot in candidateRoots)
        {
            if (!string.IsNullOrWhiteSpace(candidateRoot))
                return candidateRoot;
        }

        throw new ArgumentException(
            "Cannot resolve config path: no explicit path was provided and both currentDirectory and appBaseDirectory are null or empty — nowhere to search for the config directory.",
            "currentDirectory");
    }

    private static string BuildConfigPath(string root) =>
        Path.Combine(Path.GetFullPath(root), ConfigDirectoryName, ConfigFileName);

    private static string BuildRepositoryMarkerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), RepositoryMarkerFileName);


    private static List<LaunchEntry> NormalizeEntries(IEnumerable<LaunchEntry?> entries)
    {
        List<LaunchEntry> normalized = [];
        var seenIds = new HashSet<string>(LauncherEntryIds.Comparer);
        var index = 0;
        foreach (var entry in entries)
        {
            if (index >= 10000)
                throw new InvalidDataException("Launcher config exceeds 10000 entry limit.");
            normalized.Add(Normalize(RequireEntry(entry, index), index, seenIds));
            index++;
        }

        return normalized;
    }

    private static LaunchEntry RequireEntry(LaunchEntry? entry, int index) =>
        entry ?? throw new InvalidDataException($"Launcher config entry {index} is null.");

    private static LaunchEntry Normalize(LaunchEntry entry, int index, HashSet<string> seenIds)
    {
        // LaunchEntry is immutable ({ get; init; }) — normalization returns a new instance
        // instead of mutating, making every data transformation explicit and traceable.
        return new LaunchEntry
        {
            Id = RequireEntryId(entry.Id, index, seenIds),
            Name = RequireEntryText(entry.Name, index, "name"),
            Directory = RequireEntryText(entry.Directory, index, "directory"),
            Command = RequireEntryText(entry.Command, index, "command"),
            UpdatedAtUtc = NormalizeTimestampEntryText(entry.UpdatedAtUtc, index, "updatedAtUtc"),
            LaunchedAtUtc = NormalizeTimestampEntryText(entry.LaunchedAtUtc, index, "launchedAtUtc"),
            Selected = entry.Selected,
        };
    }

    private static string RequireEntryId(string? id, int index, HashSet<string> seenIds)
    {
        var normalized = LauncherEntryIds.RequireEntryId(id, nameof(id));

        if (!seenIds.Add(normalized))
            throw new InvalidDataException($"Launcher config entry {index} id is duplicate: {normalized}.");

        return normalized;
    }

    private static string RequireEntryText(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher config entry {index} {fieldName} is null.");

        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length > 4096)
            throw new InvalidDataException($"Launcher config entry {index} {fieldName} exceeds 4096 character length limit.");
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher config entry {index} {fieldName} is empty.");

        return normalized;
    }

    private static string NormalizeTimestampEntryText(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher config entry {index} {fieldName} is null.");

        return LauncherEntryText.Normalize(value);
    }
}
