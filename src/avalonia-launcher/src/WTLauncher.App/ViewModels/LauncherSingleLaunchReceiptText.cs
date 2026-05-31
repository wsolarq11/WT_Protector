using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        ArgumentNullException.ThrowIfNull(plan);

        var normalizedName = LauncherEntryText.Normalize(entryName);
        if (normalizedName.Length == 0)
            normalizedName = "(unnamed)";

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已请求启动: {normalizedName}",
            plan.CommandLine);
    }
}
