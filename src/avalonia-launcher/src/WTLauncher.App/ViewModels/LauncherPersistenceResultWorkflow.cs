namespace WTLauncher.App.ViewModels;

public static class LauncherPersistenceResultWorkflow
{
    public static bool Apply(
        LauncherPersistenceStatusResult result,
        Action<string> updateStatus)
    {
        ArgumentNullException.ThrowIfNull(updateStatus);

        if (!result.Succeeded && result.StatusText is not null)
            updateStatus(result.StatusText);

        return result.Succeeded;
    }
}
