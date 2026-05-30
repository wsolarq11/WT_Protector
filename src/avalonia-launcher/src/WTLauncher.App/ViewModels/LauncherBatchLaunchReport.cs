namespace WTLauncher.App.ViewModels;

public sealed class LauncherBatchLaunchReport
{
    private readonly List<string> _failures = [];

    public int StartedCount { get; private set; }

    public int FailedCount => _failures.Count;

    public IReadOnlyList<string> FailureMessages => _failures;

    public void RecordStarted()
    {
        StartedCount++;
    }

    public void RecordFailure(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add($"{entryName}: {exception.Message}");
    }
}
