using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已请求启动: {NormalizeName(entryName)}",
            plan.CommandLine);
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {NormalizeName(entryName)}",
            plan.CommandLine);
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动确认失败: {NormalizeName(entryName)}",
            plan.CommandLine,
            $"错误: {exception.Message}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {NormalizeName(entryName)}",
            $"错误: {exception.Message}");
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {NormalizeName(entryName)}",
            plan.CommandLine,
            $"错误: {exception.Message}");
    }

    private static string NormalizeName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = LauncherEntryText.Normalize(entryName);
        return normalizedName.Length == 0 ? "(unnamed)" : normalizedName;
    }
}
