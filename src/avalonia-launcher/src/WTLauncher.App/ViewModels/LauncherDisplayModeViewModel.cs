using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherDisplayModeViewModel(LauncherEntryViewMode Mode, string Label)
{
    public string Id => Mode switch
    {
        LauncherEntryViewMode.Details => "details",
        LauncherEntryViewMode.Names => "names",
        _ => "cards",
    };

    public override string ToString()
    {
        return Label;
    }
}
