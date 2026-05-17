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

    public string Description => Preset.Description;

    public IReadOnlyList<string> EntryIds => Preset.EntryIds;

    public bool IsLocked => Preset.Locked;

    public string Label => IsLocked ? $"{Name} 🔒 ({EntryIds.Count})" : $"{Name} ({EntryIds.Count})";

    public override string ToString()
    {
        return Label;
    }
}
