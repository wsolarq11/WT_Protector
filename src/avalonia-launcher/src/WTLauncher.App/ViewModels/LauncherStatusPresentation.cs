using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

public static class LauncherStatusPresentation
{
    public static string IndicatorFor(LauncherStatusTone tone) => tone switch
    {
        LauncherStatusTone.Success => "✓",
        LauncherStatusTone.Warning => "!",
        LauncherStatusTone.Error => "×",
        _ => "•",
    };
}
