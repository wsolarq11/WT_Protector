using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherConfigStore
{
    public const string ConfigDirectoryName = "config";
    public const string ConfigFileName = "config.json";
    public const string SettingsFileName = "settings.json";
    public const string RepositoryMarkerFileName = "README.md";

    public static IReadOnlyList<LaunchEntry> Load(string configPath)
    {
        ThrowIfConfigPathMissing(configPath);

        // Single-pass load: deserialize directly from the file stream,
        // then validate on the typed objects. This eliminates the separate
        // JsonDocument.Parse pre-validation pass that doubled peak memory
        // (raw bytes + JsonDocument tree + deserialized objects) for config
        // files up to the 10 MB cap.
        IReadOnlyList<LaunchEntry> LoadAndValidate(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 10 * 1024 * 1024)
                throw new InvalidDataException("Launcher config file exceeds 10 MB size limit.");

            using var stream = File.OpenRead(path);
            var entries = JsonSerializer.Deserialize<List<LaunchEntry?>>(stream, LauncherJson.Options)
                ?? throw new InvalidDataException("Launcher config parsed to null.");

            return NormalizeEntries(entries).AsReadOnly();
        }

        return LauncherJson.LoadWithRecovery(configPath, LoadAndValidate, "ConfigLoad.RecoveredFromBackup", Array.Empty<LaunchEntry>());
    }

    public static void Save(IEnumerable<LaunchEntry> entries, string configPath, bool backup)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ThrowIfConfigPathMissing(configPath);

        LauncherJson.Save(
            NormalizeEntries(entries!),
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
