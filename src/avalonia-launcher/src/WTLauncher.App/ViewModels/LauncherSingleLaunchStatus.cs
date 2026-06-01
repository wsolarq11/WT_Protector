namespace WTLauncher.App.ViewModels;

public static class LauncherSingleLaunchStatus
{
    public static string Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? $"预演: {entryName}（需要确认）"
            : $"预演: {entryName}";

    public static string PreviewFailed(string entryName) =>
        $"预演失败: {entryName}";

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"预演失败: {exception.Message}";
    }

    public static string Canceled(string entryName) =>
        $"已取消: {entryName}";

    public static string Requested(string entryName) =>
        $"已请求启动: {entryName}";

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {entryName} — {exception.Message}";
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {exception.Message}";
    }
}
