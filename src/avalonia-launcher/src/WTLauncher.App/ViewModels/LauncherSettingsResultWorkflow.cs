namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsResultWorkflow
{
    public static bool Apply(
        LauncherSettingsMutationResult result,
        Action<string> updateStatus,
        Action onSuccess)
    {
        ArgumentNullException.ThrowIfNull(updateStatus);
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (!result.Succeeded)
        {
            if (result.StatusText is not null)
                updateStatus(result.StatusText);
            return false;
        }

        onSuccess();
        return true;
    }
}
