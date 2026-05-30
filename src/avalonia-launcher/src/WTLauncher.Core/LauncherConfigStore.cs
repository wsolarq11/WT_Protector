namespace WTLauncher.Core;

public static class LauncherConfigStore
{
    public const string ConfigDirectoryName = "config";
    public const string ConfigFileName = "config.json";
    public const string SettingsFileName = "settings.json";

    public static IReadOnlyList<LaunchEntry> Load(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Launcher config not found.", configPath);

        var entries = LauncherJson.Load<List<LaunchEntry?>>(configPath)
            ?? throw new InvalidDataException("Launcher config parsed to null.");

        return entries
            .OfType<LaunchEntry>()
            .Select(Normalize)
            .ToList();
    }

    public static void Save(IEnumerable<LaunchEntry> entries, string configPath, bool backup = true)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        LauncherJson.Save(entries.ToList(), configPath, backup);
    }

    public static string ResolveConfigPath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath.Trim());

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

            var config = Path.Combine(Path.GetFullPath(candidateRoot), ConfigDirectoryName, ConfigFileName);
            if (File.Exists(config))
                return config;
        }

        return Path.GetFullPath(Path.Combine(currentDirectory ?? Environment.CurrentDirectory, ConfigDirectoryName, ConfigFileName));
    }

    public static string ResolveSettingsPath(string configPath)
    {
        return ResolveSiblingPath(configPath, SettingsFileName);
    }

    internal static string ResolveSiblingPath(string configPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath.Trim()))
            ?? throw new InvalidOperationException("Config path must have a parent directory.");
        return Path.Combine(directory, fileName);
    }

    private static string? FindUpward(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            var config = Path.Combine(dir.FullName, ConfigDirectoryName, ConfigFileName);
            var readme = Path.Combine(dir.FullName, "README.md");
            if (File.Exists(config) && File.Exists(readme))
                return config;
        }

        return null;
    }

    private static LaunchEntry Normalize(LaunchEntry entry)
    {
        entry.Id ??= string.Empty;
        entry.Name ??= string.Empty;
        entry.Directory ??= string.Empty;
        entry.Command ??= string.Empty;
        entry.UpdatedAtUtc ??= string.Empty;
        return entry;
    }
}
