namespace WTLauncher.App.ViewModels;

public sealed class LauncherDisplayModeViewModel
{
    public LauncherDisplayModeViewModel(string id, string label)
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
