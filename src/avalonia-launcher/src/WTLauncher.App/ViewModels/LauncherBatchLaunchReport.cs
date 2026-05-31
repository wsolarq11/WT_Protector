using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherBatchLaunchReport
{
    private readonly List<string> _failures = [];

    public int StartedCount { get; private set; }

    public int FailedCount => _failures.Count;

    public int SkippedCount { get; private set; }

    public string? LastCommandLine { get; private set; }

    public IReadOnlyList<string> FailureMessages => _failures;

    public void RecordPlan(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        LastCommandLine = plan.CommandLine;
    }

    public void RecordStarted()
    {
        StartedCount++;
    }

    public void RecordSkipped(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Skipped count must not be negative.");

        SkippedCount += count;
    }

    public void RecordFailure(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add($"{NormalizeEntryName(entryName)}: {exception.Message}");
    }

    private static string NormalizeEntryName(string entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        return normalized.Length == 0 ? "(unnamed)" : normalized;
    }
}
