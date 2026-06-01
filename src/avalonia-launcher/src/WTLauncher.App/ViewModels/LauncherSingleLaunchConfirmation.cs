using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherSingleLaunchConfirmation
{
    public LauncherSingleLaunchConfirmation(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(plan);

        EntryId = RequireEntryId(entry.Id);
        EntryName = RequireEntryName(entry.Name);
        Plan = plan;
    }

    public string EntryId { get; }

    public string EntryName { get; }

    public LaunchPlan Plan { get; }

    private static string RequireEntryId(string? entryId)
    {
        var normalized = LauncherEntryIds.Normalize(entryId);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new InvalidOperationException("Launcher single launch confirmation entry id is required.");

        return normalized;
    }

    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher single launch confirmation entry name is required.");

        return normalized;
    }
}
