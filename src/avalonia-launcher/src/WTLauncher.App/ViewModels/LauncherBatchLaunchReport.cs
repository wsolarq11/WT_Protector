namespace WTLauncher.App.ViewModels;

public sealed class LauncherBatchLaunchReport
{
    private readonly List<string> _failures = [];

    public int StartedCount { get; private set; }

    public int FailedCount => _failures.Count;

    public void RecordStarted()
    {
        StartedCount++;
    }

    public void RecordFailure(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add($"{entryName}: {exception.Message}");
    }

    public string ToStatusText()
    {
        return FailedCount == 0
            ? $"已启动 {StartedCount} 个勾选项"
            : $"已启动 {StartedCount} 个勾选项，{FailedCount} 个失败：{FormatFailures()}";
    }

    private string FormatFailures()
    {
        var preview = string.Join("；", _failures.Take(3));
        if (_failures.Count > 3)
            preview += $"；…另有 {_failures.Count - 3} 项";

        return preview;
    }
}
