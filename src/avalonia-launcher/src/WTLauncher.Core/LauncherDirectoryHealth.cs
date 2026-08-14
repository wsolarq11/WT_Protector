using System.IO;

namespace WTLauncher.Core;

/// <summary>
/// Read-only directory health checks used to surface stale launcher entries
/// before a launch attempt. Checks are intentionally non-mutating.
/// </summary>
public static class LauncherDirectoryHealth
{
    public static bool IsMissing(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return !Directory.Exists(entry.Directory.Value);
    }

    public static int CountMissing(IEnumerable<LaunchEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var count = 0;
        foreach (var entry in entries)
        {
            if (IsMissing(entry))
                count++;
        }
        return count;
    }
}
