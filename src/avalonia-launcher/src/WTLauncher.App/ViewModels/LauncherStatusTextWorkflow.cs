namespace WTLauncher.App.ViewModels;

public static class LauncherStatusTextWorkflow
{
    public static bool Apply(string? statusText, Action<string> updateStatus)
    {
        ArgumentNullException.ThrowIfNull(updateStatus);

        if (statusText is null)
            return false;

        updateStatus(statusText);
        return true;
    }
}
