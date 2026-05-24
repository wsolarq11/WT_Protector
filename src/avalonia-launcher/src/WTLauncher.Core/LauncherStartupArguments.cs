namespace WTLauncher.Core;

public static class LauncherStartupArguments
{
    public static string? ReadConfigPath(IReadOnlyList<string>? args)
    {
        if (args is null)
            return null;

        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
                return string.IsNullOrWhiteSpace(args[i + 1]) ? null : args[i + 1];
        }

        return null;
    }
}
