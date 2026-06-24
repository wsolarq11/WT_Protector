namespace WTLauncher.Core;

public static class LauncherStartupArguments
{
    public static string? ReadConfigPath(IReadOnlyList<string>? args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? configPath = null;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (!string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Unknown startup argument: '{arg}'. Only --config <path> is supported for specifying a configuration file.",
                    nameof(args));

            if (configPath is not null)
                throw new ArgumentException("Config path flag must appear at most once.", nameof(args));

            if (i + 1 >= args.Count)
                throw new ArgumentException("Config path value is required after --config.", nameof(args));

            configPath = args[++i].Trim();
            if (configPath.Length == 0 || configPath.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Config path value is required after --config.", nameof(args));
        }

        return configPath;
    }
}
