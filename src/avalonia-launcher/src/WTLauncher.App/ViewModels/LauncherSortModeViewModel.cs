namespace WTLauncher.App.ViewModels;

public sealed class LauncherSortModeViewModel
{
    public LauncherSortModeViewModel(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }

    public string Label { get; }

    public override string ToString()
    {
        return Label;
    }
}
