using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchItem
{
    public LauncherBatchLaunchItem(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = RequireEntryId(entry.Id);
        EntryName = RequireEntryName(entry.Name);
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    public string EntryId { get; }

    public LaunchPlan Plan { get; }

    public string EntryName { get; }

    private static string RequireEntryId(string? entryId)
    {
        var normalized = LauncherEntryIds.Normalize(entryId);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new InvalidOperationException("Launch entry id is empty.");

        return normalized;
    }

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
