using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherConfigStore
{
    public const string ConfigDirectoryName = "config";
    public const string ConfigFileName = "config.json";
    public const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static IReadOnlyList<LaunchEntry> Load(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Launcher config not found.", configPath);

        using var stream = File.OpenRead(configPath);
        var entries = JsonSerializer.Deserialize<List<LaunchEntry>>(stream, Options)
            ?? throw new InvalidDataException("Launcher config parsed to null.");

        return entries;
    }

    public static void Save(IEnumerable<LaunchEntry> entries, string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(configPath))
            File.Copy(configPath, configPath + ".bak", overwrite: true);

        using var stream = File.Create(configPath);
        JsonSerializer.Serialize(stream, entries.ToList(), Options);
    }

    public static string ResolveConfigPath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);

        foreach (var candidateRoot in new[] { currentDirectory, appBaseDirectory })
        {
            var found = FindUpward(candidateRoot);
            if (found is not null)
                return found;
        }

        foreach (var candidateRoot in new[] { currentDirectory, appBaseDirectory })
        {
            var found = FindLocalConfig(candidateRoot);
            if (found is not null)
                return found;
        }

        return Path.GetFullPath(Path.Combine(currentDirectory ?? Environment.CurrentDirectory, ConfigDirectoryName, ConfigFileName));
    }

    public static string ResolveSettingsPath(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath))
            ?? throw new InvalidOperationException("Config path must have a parent directory.");
        return Path.Combine(directory, SettingsFileName);
    }

    private static string? FindLocalConfig(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        var config = Path.Combine(Path.GetFullPath(start), ConfigDirectoryName, ConfigFileName);
        return File.Exists(config) ? config : null;
    }

    private static string? FindUpward(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        var dir = new DirectoryInfo(Path.GetFullPath(start));
        while (dir is not null)
        {
            var config = Path.Combine(dir.FullName, ConfigDirectoryName, ConfigFileName);
            var readme = Path.Combine(dir.FullName, "README.md");
            if (File.Exists(config) && File.Exists(readme))
                return config;
            dir = dir.Parent;
        }

        return null;
    }
}
