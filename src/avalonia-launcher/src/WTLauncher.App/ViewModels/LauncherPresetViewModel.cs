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

    public bool IsLocked => Preset.Locked;

    public string Label => IsLocked ? $"{Name} (系统组, {EntryIds.Count}项)" : $"{Name} ({EntryIds.Count}项)";

    public override string ToString() => Label;
}
