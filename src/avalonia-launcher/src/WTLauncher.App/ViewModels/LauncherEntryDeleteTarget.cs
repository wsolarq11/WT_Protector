using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherEntryDeleteTarget
{
    public LauncherEntryDeleteTarget(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = LauncherEntryIds.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = LauncherEntryText.RequireNonEmpty(entry.Name);
    }

    public string EntryId { get; }

    public string EntryName { get; }
}
