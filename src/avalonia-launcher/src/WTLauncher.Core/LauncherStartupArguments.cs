namespace WTLauncher.Core;

public static class LauncherStartupArguments
{
    /// <summary>
    /// Parses startup arguments and returns the explicit config path (if any) along
    /// with any unknown "--" prefixed arguments collected as warnings rather than
    /// fatal errors. Callers should report these to the user after startup.
    /// </summary>
    public static (string? configPath, IReadOnlyList<string> unknownArgs) ReadConfigPath(IReadOnlyList<string>? args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var unknownArgs = new List<string>();

        string? configPath = null;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            // Support --config=path in addition to --config path.
            if (arg.StartsWith("--config=", StringComparison.OrdinalIgnoreCase))
            {
                if (configPath is not null)
                    throw new ArgumentException("Config path flag must appear at most once.", nameof(args));

                configPath = arg["--config=".Length..].Trim();
                if (configPath.Length == 0 || configPath.StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException("Config path value is required after --config.", nameof(args));
                if (configPath.StartsWith(@"\\", StringComparison.Ordinal) || configPath.StartsWith("//", StringComparison.Ordinal))
                    throw new ArgumentException("Network/UNC paths are not allowed for the launcher configuration.", nameof(args));
                continue;
            }

            if (!string.Equals(arg, "--config", StringComparison.OrdinalIgnoreCase))
            {
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    unknownArgs.Add(arg);
                    continue;
                }

                throw new ArgumentException(
                    $"Unknown startup argument: '{arg}'. Only --config <path> is supported for specifying a configuration file.",
                    nameof(args));
            }

            if (configPath is not null)
                throw new ArgumentException("Config path flag must appear at most once.", nameof(args));

            if (i + 1 >= args.Count)
                throw new ArgumentException("Config path value is required after --config.", nameof(args));

            configPath = args[++i].Trim();
            if (configPath.Length == 0 || configPath.StartsWith("-", StringComparison.Ordinal))
                throw new ArgumentException("Config path value is required after --config.", nameof(args));
            if (configPath.StartsWith(@"\\", StringComparison.Ordinal) || configPath.StartsWith("//", StringComparison.Ordinal))
                throw new ArgumentException("Network/UNC paths are not allowed for the launcher configuration.", nameof(args));
        }

        return (configPath, unknownArgs.AsReadOnly());
    }
}
