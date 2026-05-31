namespace WTLauncher.Core;

public static class LauncherStartupArguments
{
    public static string? ReadConfigPath(IReadOnlyList<string>? args)
    {
        if (args is null)
            return null;

        string? configPath = null;
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase))
                continue;

            if (configPath is not null)
                throw new ArgumentException("Config path flag must appear at most once.", nameof(args));

            if (i + 1 >= args.Count)
                throw new ArgumentException("Config path value is required after --config.", nameof(args));

            configPath = args[i + 1].Trim();
            if (configPath.Length == 0 || configPath.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Config path value is required after --config.", nameof(args));
        }

        return configPath;
    }
}
