using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetViewModel
{
    public LauncherPresetViewModel(LauncherPreset preset)
    {
        Preset = preset;
    }

    public LauncherPreset Preset { get; }

    public string Id => Preset.Id;

    public string Name => Preset.Name;

    public IReadOnlyList<string> EntryIds => Preset.EntryIds;

    public int EntryCount => LauncherEntryIds.ToRequiredUniqueList(EntryIds, nameof(EntryIds)).Count;

    public bool IsLocked => Preset.Locked;

    public string Label => IsLocked ? $"{Name} (系统组, {EntryCount}项)" : $"{Name} ({EntryCount}项)";

    public override string ToString() => Label;
}
