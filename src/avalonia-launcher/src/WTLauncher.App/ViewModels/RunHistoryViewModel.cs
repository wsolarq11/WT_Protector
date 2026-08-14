using System.Collections.ObjectModel;
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

        Summary = $"共 {history.Count} 条启动记录";
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
        nameof(LauncherRunSource.Individual) => "单条",
        nameof(LauncherRunSource.Selection) => "勾选",
        nameof(LauncherRunSource.Preset) => "启动组",
        nameof(LauncherRunSource.Group) => "分组",
        _ => source,
    };

    private static string StatusLabel(string status) => status switch
    {
        nameof(LauncherRunStatus.Requested) => "已请求",
        nameof(LauncherRunStatus.Failed) => "失败",
        nameof(LauncherRunStatus.Skipped) => "跳过",
        nameof(LauncherRunStatus.Canceled) => "取消",
        _ => status,
    };
}
