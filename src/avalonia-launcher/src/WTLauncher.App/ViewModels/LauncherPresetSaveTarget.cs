using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherPresetSaveTarget
{
    public LauncherPresetSaveTarget(IEnumerable<string> entryIds)
    {
        var normalizedEntryIds = EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        if (normalizedEntryIds.Count == 0)
            throw new InvalidOperationException("Launcher preset save target entry ids are required.");

        EntryIds = new ReadOnlyCollection<string>(normalizedEntryIds);
    }

    public IReadOnlyList<string> EntryIds { get; }

    public int EntryCount => EntryIds.Count;
}
