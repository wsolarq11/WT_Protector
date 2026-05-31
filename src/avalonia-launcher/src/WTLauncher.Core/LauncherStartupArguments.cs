namespace WTLauncher.Core;

public static class LauncherStartupArguments
{
    public static string? ReadConfigPath(IReadOnlyList<string>? args)
    {
        if (args is null)
            return null;

        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 >= args.Count)
                throw new ArgumentException("Config path value is required after --config.", nameof(args));

            var configPath = args[i + 1].Trim();
            if (configPath.Length == 0 || configPath.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Config path value is required after --config.", nameof(args));

            return configPath;
        }

        return null;
    }
}
