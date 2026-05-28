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

    public string Id => Mode switch
    {
        LauncherEntryViewMode.Details => "details",
        LauncherEntryViewMode.Names => "names",
        _ => "cards",
    };

    public string Label { get; }

    public override string ToString()
    {
        return Label;
    }
}
