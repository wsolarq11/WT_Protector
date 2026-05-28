namespace WTLauncher.App.ViewModels;

public sealed record LauncherSortModeViewModel(string Id, string Label)
{
    public override string ToString()
    {
        return Label;
    }
}
