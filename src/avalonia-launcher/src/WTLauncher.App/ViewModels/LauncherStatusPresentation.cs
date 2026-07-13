namespace WTLauncher.App.ViewModels;

public enum LauncherStatusTone
{
    Neutral,
    Success,
    Warning,
    Error,
}

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
