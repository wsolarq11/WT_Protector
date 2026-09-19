using System.Collections.ObjectModel;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class RunHistoryViewModel
{
    public ObservableCollection<RunHistoryRowViewModel> Rows { get; } = [];

    public string Summary { get; }

    public RunHistoryViewModel(IReadOnlyList<LauncherRunHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        foreach (var entry in history
                     .OrderByDescending(item => item.TimestampUtc, StringComparer.Ordinal))
        {
            Rows.Add(new RunHistoryRowViewModel(entry));
        }

        Summary = LauncherLocalization.Format("runHistory.summary", history.Count);
    }
}

public sealed class RunHistoryRowViewModel
{
    public string Time { get; }

    public string EntryName { get; }

    public string EntryId { get; }

    public string Source { get; }

    public string Status { get; }

    public bool IsRequested { get; }

    public bool IsFailed { get; }

    public bool IsSkipped { get; }

    public bool IsCanceled { get; }

    public RunHistoryRowViewModel(LauncherRunHistoryEntry entry)
    {
        Time = entry.TimestampUtc;
        EntryName = entry.EntryName;
        EntryId = entry.EntryId;
        Source = SourceLabel(entry.Source);
        Status = StatusLabel(entry.Status);
        IsRequested = entry.Status == nameof(LauncherRunStatus.Requested);
        IsFailed = entry.Status == nameof(LauncherRunStatus.Failed);
        IsSkipped = entry.Status == nameof(LauncherRunStatus.Skipped);
        IsCanceled = entry.Status == nameof(LauncherRunStatus.Canceled);
    }

    private static string SourceLabel(string source) => source switch
    {
        nameof(LauncherRunSource.Individual) => LauncherLocalization.Format("runHistory.sourceIndividual"),
        nameof(LauncherRunSource.Selection) => LauncherLocalization.Format("runHistory.sourceSelection"),
        nameof(LauncherRunSource.Preset) => LauncherLocalization.Format("runHistory.sourcePreset"),
        nameof(LauncherRunSource.Group) => LauncherLocalization.Format("runHistory.sourceGroup"),
        _ => source,
    };

    private static string StatusLabel(string status) => status switch
    {
        nameof(LauncherRunStatus.Requested) => LauncherLocalization.Format("runHistory.statusRequested"),
        nameof(LauncherRunStatus.Failed) => LauncherLocalization.Format("runHistory.statusFailed"),
        nameof(LauncherRunStatus.Skipped) => LauncherLocalization.Format("runHistory.statusSkipped"),
        nameof(LauncherRunStatus.Canceled) => LauncherLocalization.Format("runHistory.statusCanceled"),
        _ => status,
    };
}
