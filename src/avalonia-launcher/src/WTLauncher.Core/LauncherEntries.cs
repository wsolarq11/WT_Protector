namespace WTLauncher.Core;

public static class LauncherEntries
{
    public static LaunchEntry Clone(LaunchEntry entry)
    {
        return new LaunchEntry
        {
            Id = entry.Id,
            Name = entry.Name,
            Directory = entry.Directory,
            Command = entry.Command,
            Confirm = entry.Confirm,
            Selected = entry.Selected,
            UpdatedAtUtc = entry.UpdatedAtUtc,
        };
    }

    public static List<LaunchEntry> Clone(IEnumerable<LaunchEntry> entries)
    {
        return entries.Select(Clone).ToList();
    }
}
