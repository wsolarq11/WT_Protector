using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchItem
{
    public LauncherBatchLaunchItem(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entry = entry;
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        EntryName = RequireEntryName(entry.Name);
    }

    public LaunchEntry Entry { get; }

    public LaunchPlan Plan { get; }

    public string EntryName { get; }

    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");

        return normalized;
    }
}

public sealed record LauncherBatchLaunchPreflight(
    IReadOnlyList<LauncherBatchLaunchItem> Items,
    LauncherBatchLaunchReport Report);
