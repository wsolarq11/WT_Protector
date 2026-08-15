using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

/// <summary>Narrow status output port for command workflows.</summary>
public interface IStatusSink
{
    public void Apply(LauncherStatusContent status);
    public void SetDryRunText(string text);
}
