namespace WTLauncher.Core;

public static class LauncherEntrySearch
{
    public static bool Matches(LaunchEntry entry, string? filter)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var needle = (filter ?? string.Empty).Trim();
        if (needle.Length == 0)
            return true;

        return entry.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || entry.Directory.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || entry.Command.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
