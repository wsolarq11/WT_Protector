using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchReport
{
    private readonly IReadOnlyList<string> _failureMessages;
    private readonly IReadOnlyList<string> _failureEntryIds;
    private readonly IReadOnlyList<string> _failureEntryNames;

    public LauncherBatchLaunchReport(
        int requestedCount = 0,
        int skippedCount = 0,
        string? lastCommandLine = null,
        int? failureIndex = null,
        IReadOnlyList<int>? failureIndices = null,
        IEnumerable<string>? failureMessages = null,
        IEnumerable<string>? failureEntryIds = null,
        IEnumerable<string>? failureEntryNames = null)
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
        FailureIndices = failureIndices ?? Array.Empty<int>();
        _failureMessages = Array.AsReadOnly(CloneFailureMessages(failureMessages));
        _failureEntryIds = Array.AsReadOnly(CloneFailureEntryIds(failureEntryIds));
        _failureEntryNames = Array.AsReadOnly(CloneFailureEntryNames(failureEntryNames));
    }

    public int RequestedCount { get; }

    public int FailedCount => _failureMessages.Count;

    public int SkippedCount { get; }

    public string? LastCommandLine { get; }

    public int? FailureIndex { get; }

    /// <summary>
    /// All failure indices. Used by receipt formatting to correctly determine item
    /// status regardless of failure ordering within the batch.
    /// </summary>
    public IReadOnlyList<int> FailureIndices { get; }

    public IReadOnlyList<string> FailureMessages => _failureMessages;

    public IReadOnlyList<string> FailureEntryIds => _failureEntryIds;

    public IReadOnlyList<string> FailureEntryNames => _failureEntryNames;

    private static string[] CloneFailureMessages(IEnumerable<string>? failureMessages)
    {
        if (failureMessages is null)
            return [];

        List<string> clone = [];
        foreach (var failure in failureMessages)
        {
            if (failure is null)
                throw new InvalidDataException("Launcher batch failure message is null.");

            clone.Add(failure);
        }

        return [.. clone];
    }

    private static string[] CloneFailureEntryIds(IEnumerable<string>? failureEntryIds)
    {
        if (failureEntryIds is null)
            return [];

        List<string> clone = [];
        foreach (var entryId in failureEntryIds)
        {
            if (entryId is null)
                throw new InvalidDataException("Launcher batch failure entry id is null.");

            clone.Add(entryId);
        }

        return [.. clone];
    }

    private static string[] CloneFailureEntryNames(IEnumerable<string>? failureEntryNames)
    {
        if (failureEntryNames is null)
            return [];

        List<string> clone = [];
        foreach (var entryName in failureEntryNames)
        {
            if (entryName is null)
                throw new InvalidDataException("Launcher batch failure entry name is null.");

            clone.Add(entryName);
        }

        return [.. clone];
    }
}

internal sealed class LauncherBatchLaunchReportBuilder
{
    private readonly List<string> _failures = [];
    private readonly List<int> _failureIndices = [];
    private readonly List<string> _failureEntryIds = [];
    private readonly List<string> _failureEntryNames = [];

    public LauncherBatchLaunchReportBuilder()
    {
    }

    public LauncherBatchLaunchReportBuilder(LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        RequestedCount = report.RequestedCount;
        SkippedCount = report.SkippedCount;
        LastCommandLine = report.LastCommandLine;
        FailureIndex = report.FailureIndex;
        _failureIndices.AddRange(report.FailureIndices);
        _failures.AddRange(report.FailureMessages);
        _failureEntryIds.AddRange(report.FailureEntryIds);
        _failureEntryNames.AddRange(report.FailureEntryNames);
    }

    public int RequestedCount { get; private set; }

    public int FailedCount => _failures.Count;

    public int SkippedCount { get; private set; }

    public string? LastCommandLine { get; private set; }

    public int? FailureIndex { get; private set; }

    public LauncherBatchLaunchReport ToReport() =>
        new(RequestedCount, SkippedCount, LastCommandLine, FailureIndex, _failureIndices, _failures, _failureEntryIds, _failureEntryNames);

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

    public void RecordFailure(string entryId, string entryName, Exception exception)
    {
        RecordFailure(entryId, entryName, failureIndex: null, exception);
    }

    public void RecordFailure(string entryId, string entryName, int failureIndex, Exception exception)
    {
        RecordFailure(entryId, entryName, (int?)failureIndex, exception);
    }

    private void RecordFailure(string entryId, string entryName, int? failureIndex, Exception exception)
    {
        if (!string.IsNullOrWhiteSpace(entryId))
        {
            _failureEntryIds.Add(entryId);
            _failureEntryNames.Add(EntryName.Normalize(entryName));
        }
        RecordFailure(entryName, failureIndex, exception);
    }

    private void RecordFailure(string entryName, int? failureIndex, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add($"{EntryName.RequireNonEmpty(entryName)}: {ExceptionMessageFormatter.ForUser(exception)}");
        FailureIndex ??= failureIndex;
        if (failureIndex is not null)
            _failureIndices.Add(failureIndex.Value);
    }

    public void RecordFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _failures.Add(ExceptionMessageFormatter.ForUser(exception));
    }
}
