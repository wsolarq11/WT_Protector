using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDisplayModeViewModel
{
    public LauncherDisplayModeViewModel(LauncherEntryViewMode mode, string label)
    {
        Mode = mode;
        Label = label;
    }

    public LauncherEntryViewMode Mode { get; }

    public string Id => Mode.ToSettingsValue();

    public string Label { get; }

    public override string ToString()
    {
        return Label;
    }
}
