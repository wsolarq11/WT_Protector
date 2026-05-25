namespace WTLauncher.App.ViewModels;

public static class LauncherPresetManagementResultWorkflow
{
    public static void Apply(
        LauncherPresetManagementResult result,
        Action<string?> reloadPresets,
        Action<string> updateStatus)
    {
        ArgumentNullException.ThrowIfNull(reloadPresets);
        ArgumentNullException.ThrowIfNull(updateStatus);

        if (result.ShouldReload)
            reloadPresets(result.NextPresetId);
        if (result.StatusText is not null)
            updateStatus(result.StatusText);
    }
}
