using WTLauncher.Core;
using _EntryId = WTLauncher.Core.EntryId;
using _EntryName = WTLauncher.Core.EntryName;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherSingleLaunchConfirmation
{
    public LauncherSingleLaunchConfirmation(LaunchEntry entry, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(plan);

        EntryId = _EntryId.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = _EntryName.RequireNonEmpty(entry.Name);
        Plan = plan;
    }

    public string EntryId { get; }

    public string EntryName { get; }

    public LaunchPlan Plan { get; }
}
