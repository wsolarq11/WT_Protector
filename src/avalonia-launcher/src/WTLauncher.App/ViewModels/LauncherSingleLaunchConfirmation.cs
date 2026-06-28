using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherSingleLaunchConfirmation
{
    public LauncherSingleLaunchConfirmation(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(plan);

        EntryId = LauncherEntryIds.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = LauncherEntryText.RequireNonEmpty(entry.Name);
        Plan = plan;
    }

    public string EntryId { get; }

    public string EntryName { get; }

    public LaunchPlan Plan { get; }
}
