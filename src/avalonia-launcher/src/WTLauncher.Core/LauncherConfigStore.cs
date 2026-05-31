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

        var entries = LauncherJson.LoadRequired<List<LaunchEntry?>>(configPath, "Launcher config");

        return NormalizeEntries(entries);
    }

    public static void Save(IEnumerable<LaunchEntry> entries, string configPath, bool backup)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ThrowIfConfigPathMissing(configPath);

        LauncherJson.Save(
            CloneNormalizedEntries(entries),
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

            return Path.GetFullPath(trimmedExplicitPath);
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

        return BuildConfigPath(currentDirectory ?? Environment.CurrentDirectory);
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

    private static string BuildConfigPath(string root) =>
        Path.Combine(Path.GetFullPath(root), ConfigDirectoryName, ConfigFileName);

    private static string BuildRepositoryMarkerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), RepositoryMarkerFileName);

    private static List<LaunchEntry> NormalizeEntries(IEnumerable<LaunchEntry?> entries)
    {
        var normalized = new List<LaunchEntry>();
        var seenIds = new HashSet<string>(LauncherEntryIds.Comparer);
        var index = 0;
        foreach (var entry in entries)
        {
            normalized.Add(Normalize(RequireEntry(entry, index), index, seenIds));
            index++;
        }

        return normalized;
    }

    private static List<LaunchEntry> CloneNormalizedEntries(IEnumerable<LaunchEntry> entries)
    {
        var normalized = new List<LaunchEntry>();
        var seenIds = new HashSet<string>(LauncherEntryIds.Comparer);
        var index = 0;
        foreach (var entry in entries)
        {
            normalized.Add(CloneNormalized(RequireEntry(entry, index), index, seenIds));
            index++;
        }

        return normalized;
    }

    private static LaunchEntry RequireEntry(LaunchEntry? entry, int index) =>
        entry ?? throw new InvalidDataException($"Launcher config entry {index} is null.");

    private static LaunchEntry Normalize(LaunchEntry entry, int index, HashSet<string> seenIds)
    {
        entry.Id = RequireEntryId(entry.Id, index, seenIds);
        entry.Name = RequireEntryText(entry.Name, index, "name");
        entry.Directory = RequireEntryText(entry.Directory, index, "directory");
        entry.Command = RequireEntryText(entry.Command, index, "command");
        entry.UpdatedAtUtc = NormalizeOptionalEntryText(entry.UpdatedAtUtc, index, "updatedAtUtc");
        return entry;
    }

    private static LaunchEntry CloneNormalized(LaunchEntry entry, int index, HashSet<string> seenIds) =>
        Normalize(entry with { }, index, seenIds);

    private static string RequireEntryId(string? id, int index, HashSet<string> seenIds)
    {
        if (id is null)
            throw new InvalidDataException($"Launcher config entry {index} id is null.");

        var normalized = LauncherEntryIds.Normalize(id);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher config entry {index} id is empty.");

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

    private static string NormalizeOptionalEntryText(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher config entry {index} {fieldName} is null.");

        return LauncherEntryText.Normalize(value);
    }
}
