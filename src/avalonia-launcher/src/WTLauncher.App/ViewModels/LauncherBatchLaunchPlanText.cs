namespace WTLauncher.App.ViewModels;

public static class LauncherBatchLaunchPlanText
{
    public static string Format(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            items.Select(item => $"• {item.Entry.Name}{Environment.NewLine}{item.Plan.CommandLine}"));
    }
}
