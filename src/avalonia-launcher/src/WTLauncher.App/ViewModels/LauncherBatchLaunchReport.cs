using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchReport
{
    private readonly IReadOnlyList<string> _failureMessages;

    public LauncherBatchLaunchReport(
        int requestedCount = 0,
        int skippedCount = 0,
        string? lastCommandLine = null,
        int? failureIndex = null,
        IEnumerable<string>? failureMessages = null)
    {
        if (requestedCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(requestedCount),
                requestedCount,
                "Requested count must not be negative.");
        if (skippedCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(skippedCount),
                skippedCount,
                "Skipped count must not be negative.");

        RequestedCount = requestedCount;
        SkippedCount = skippedCount;
        LastCommandLine = lastCommandLine;
        FailureIndex = failureIndex;
        _failureMessages = Array.AsReadOnly(CloneFailureMessages(failureMessages));
    }

    public int RequestedCount { get; }

    public int FailedCount => _failureMessages.Count;

    public int SkippedCount { get; }

    public string? LastCommandLine { get; }

    public int? FailureIndex { get; }

    public IReadOnlyList<string> FailureMessages => _failureMessages;

    private static string[] CloneFailureMessages(IEnumerable<string>? failureMessages)
    {
        if (failureMessages is null)
            return [];

        var clone = new List<string>();
        foreach (var failure in failureMessages)
        {
            if (failure is null)
                throw new InvalidDataException("Launcher batch failure message is null.");

            clone.Add(failure);
        }

        return [.. clone];
    }
}

internal sealed class LauncherBatchLaunchReportBuilder
{
    private readonly List<string> _failures = [];

    public LauncherBatchLaunchReportBuilder()
    {
    }

    public LauncherBatchLaunchReportBuilder(LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        RequestedCount = report.RequestedCount;
        SkippedCount = report.SkippedCount;
        LastCommandLine = report.LastCommandLine;
        _failures.AddRange(report.FailureMessages);
    }

    public int RequestedCount { get; private set; }

    public int FailedCount => _failures.Count;

    public int SkippedCount { get; private set; }

    public string? LastCommandLine { get; private set; }

    public int? FailureIndex { get; private set; }

    public LauncherBatchLaunchReport ToReport() =>
        new(RequestedCount, SkippedCount, LastCommandLine, FailureIndex, _failures);

    public void RecordPlan(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        LastCommandLine = plan.CommandLine;
    }

    public void RecordRequested()
    {
        RequestedCount++;
    }

    public void RecordSkipped(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Skipped count must not be negative.");

        SkippedCount += count;
    }

    public void RecordFailure(string entryName, Exception exception)
    {
        RecordFailure(entryName, failureIndex: null, exception);
    }

    public void RecordFailure(string entryName, int failureIndex, Exception exception)
    {
        RecordFailure(entryName, (int?)failureIndex, exception);
    }

    private void RecordFailure(string entryName, int? failureIndex, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add($"{RequireEntryName(entryName)}: {exception.Message}");
        FailureIndex ??= failureIndex;
    }

    public void RecordFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add(exception.Message);
    }

    private static string RequireEntryName(string entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidDataException("Entry name is required for batch launch report.");

        return normalized;
    }
}
