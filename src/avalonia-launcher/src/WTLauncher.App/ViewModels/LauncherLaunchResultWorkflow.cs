namespace WTLauncher.App.ViewModels;

public static class LauncherLaunchResultWorkflow
{
    public static void Apply(
        string? dryRunText,
        string statusText,
        Action<string> updateStatus,
        Action<string>? updateDryRunText = null)
    {
        ArgumentNullException.ThrowIfNull(statusText);
        ArgumentNullException.ThrowIfNull(updateStatus);

        if (dryRunText is not null && updateDryRunText is not null)
            updateDryRunText(dryRunText);

        updateStatus(statusText);
    }
}
