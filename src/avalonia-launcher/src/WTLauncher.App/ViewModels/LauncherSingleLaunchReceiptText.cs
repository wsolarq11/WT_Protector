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
            $"已请求启动: {RequireName(entryName)}",
            plan.CommandLine);
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {RequireName(entryName)}",
            plan.CommandLine);
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动确认失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {exception.Message}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            $"错误: {exception.Message}");
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {exception.Message}");
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {exception.Message}");
    }

    private static string RequireName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = LauncherEntryText.Normalize(entryName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Entry name is required for single launch receipt.", nameof(entryName));

        return normalizedName;
    }
}
