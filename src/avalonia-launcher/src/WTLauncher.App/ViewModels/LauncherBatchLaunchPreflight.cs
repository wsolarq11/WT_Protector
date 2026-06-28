using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchItem
{
    public LauncherBatchLaunchItem(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = LauncherEntryIds.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = LauncherEntryText.RequireNonEmpty(entry.Name);
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    public string EntryId { get; }

    public LaunchPlan Plan { get; }

    public string EntryName { get; }
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
        string targetLabel,
        int failedCount = 0,
        IReadOnlyList<string>? failureMessages = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items.ToArray();
        foreach (var item in Items)
            ArgumentNullException.ThrowIfNull(item);

        TargetLabel = RequireTargetLabel(targetLabel);
        FailedCount = failedCount >= 0
            ? failedCount
            : throw new ArgumentOutOfRangeException(nameof(failedCount), failedCount, "Failed count must not be negative.");
        FailureMessages = failureMessages?.ToArray() ?? [];
    }

    public IReadOnlyList<LauncherBatchLaunchItem> Items { get; }

    public string TargetLabel { get; }

    public int FailedCount { get; }

    public IReadOnlyList<string> FailureMessages { get; }

    private static string RequireTargetLabel(string? targetLabel)
    {
        var normalized = LauncherEntryText.Normalize(targetLabel);
        if (normalized.Length == 0)
            throw new ArgumentException("Batch launch confirmation target label is required.", nameof(targetLabel));

        return normalized;
    }
}
