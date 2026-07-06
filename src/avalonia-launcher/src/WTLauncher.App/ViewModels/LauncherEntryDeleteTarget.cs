using WTLauncher.Core;
using _EntryId = WTLauncher.Core.EntryId;
using _EntryName = WTLauncher.Core.EntryName;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherEntryDeleteTarget
{
    public LauncherEntryDeleteTarget(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = _EntryId.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = _EntryName.RequireNonEmpty(entry.Name);
    }

    public string EntryId { get; }

    public string EntryName { get; }
}
