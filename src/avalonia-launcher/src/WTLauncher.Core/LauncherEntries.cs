namespace WTLauncher.Core;

public static class LauncherEntries
{
    /// <summary>
    /// Creates a shallow clone of <paramref name="entry"/> using the compiler-
    /// generated <c>with</c> expression, which copies every property and ensures
    /// future fields added to <see cref="LaunchEntry"/> are included automatically.
    /// </summary>
    public static LaunchEntry Clone(LaunchEntry entry)
    {
        return entry with { };
    }

    public static List<LaunchEntry> Clone(IEnumerable<LaunchEntry> entries)
    {
        return entries.Select(Clone).ToList();
    }
}
