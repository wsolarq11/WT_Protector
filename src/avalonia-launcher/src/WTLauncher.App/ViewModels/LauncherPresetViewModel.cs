using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetViewModel
{
    public LauncherPresetViewModel(LauncherPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        Id = preset.Id;
        Name = preset.Name;
        EntryIds = new ReadOnlyCollection<string>(SnapshotEntryIds(preset.EntryIds));
        EntryCount = LauncherEntryIds.ToRequiredUniqueList(EntryIds, nameof(EntryIds)).Count;
        IsLocked = preset.Locked;
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<string> EntryIds { get; }

    public int EntryCount { get; }

    public bool IsLocked { get; }

    public string Label => IsLocked ? $"{Name} (系统组, {EntryCount}项)" : $"{Name} ({EntryCount}项)";

    public override string ToString() => Label;

    private static List<string> SnapshotEntryIds(IEnumerable<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(entryIds, nameof(LauncherPreset.EntryIds));
        return [.. entryIds];
    }
}
