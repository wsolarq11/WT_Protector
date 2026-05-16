using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherConfigStore
{
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

        return Path.GetFullPath(Path.Combine(currentDirectory ?? Environment.CurrentDirectory, "config.json"));
    }

    private static string? FindUpward(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        var dir = new DirectoryInfo(Path.GetFullPath(start));
        while (dir is not null)
        {
            var config = Path.Combine(dir.FullName, "config.json");
            var readme = Path.Combine(dir.FullName, "README.md");
            if (File.Exists(config) && File.Exists(readme))
                return config;
            dir = dir.Parent;
        }

        return null;
    }
}
