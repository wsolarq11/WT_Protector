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
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Launcher config not found.", configPath);

        RequireEntrySelectionFields(configPath);
        var entries = LauncherJson.LoadRequired<List<LaunchEntry?>>(configPath, "Launcher config");

        return NormalizeEntries(entries).AsReadOnly();
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
            "Config search root is required when config path is not explicit.",
            "currentDirectory");
    }

    private static string BuildConfigPath(string root) =>
        Path.Combine(Path.GetFullPath(root), ConfigDirectoryName, ConfigFileName);

    private static string BuildRepositoryMarkerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), RepositoryMarkerFileName);

    private static void RequireEntrySelectionFields(string configPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(configPath), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return;

        var index = 0;
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object &&
                !entry.TryGetProperty("selected", out _))
            {
                throw new InvalidDataException($"Launcher config entry {index} selected is missing.");
            }

            index++;
        }
    }

    private static List<LaunchEntry> NormalizeEntries(IEnumerable<LaunchEntry?> entries)
    {
        List<LaunchEntry> normalized = [];
        var seenIds = new HashSet<string>(LauncherEntryIds.Comparer);
        var index = 0;
        foreach (var entry in entries)
        {
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
