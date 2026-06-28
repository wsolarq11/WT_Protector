using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherSingleLaunchConfirmation
{
    public LauncherSingleLaunchConfirmation(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(plan);

        EntryId = LauncherEntryIds.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = RequireEntryName(entry.Name);
        Plan = plan;
    }

    public string EntryId { get; }

    public string EntryName { get; }

    public LaunchPlan Plan { get; }

    // ponytail: kept inline rather than shared via LauncherEntryText to avoid coupling these small types to Core layer utilities.
    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher single launch confirmation entry name is required.");

        return normalized;
    }
}
