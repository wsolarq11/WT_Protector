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

    private static string RequireEntryId(string? entryId) =>
        LauncherEntryIds.RequireEntryId(entryId, nameof(entryId));

    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launch entry name is empty.");

        return normalized;
    }
}

public sealed record LauncherBatchLaunchPreflight
{
    public LauncherBatchLaunchPreflight(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items.ToArray();
        foreach (var item in Items)
            ArgumentNullException.ThrowIfNull(item);

        Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    public IReadOnlyList<LauncherBatchLaunchItem> Items { get; }

    public LauncherBatchLaunchReport Report { get; }
}

public sealed record LauncherBatchLaunchConfirmation
{
    public LauncherBatchLaunchConfirmation(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string targetLabel)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items.ToArray();
        foreach (var item in Items)
            ArgumentNullException.ThrowIfNull(item);

        TargetLabel = RequireTargetLabel(targetLabel);
    }

    public IReadOnlyList<LauncherBatchLaunchItem> Items { get; }

    public string TargetLabel { get; }

    private static string RequireTargetLabel(string? targetLabel)
    {
        var normalized = LauncherEntryText.Normalize(targetLabel);
        if (normalized.Length == 0)
            throw new ArgumentException("Batch launch confirmation target label is required.", nameof(targetLabel));

        return normalized;
    }
}
