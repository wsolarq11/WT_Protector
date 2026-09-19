using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>Distinguishes a store save failure that happened after an external
/// launch side effect from one that happened before the launch request.</summary>
public enum LaunchSaveResultKind
{
    SaveSucceeded,
    SaveFailedAfterLaunch,
    SaveFailedBeforeLaunch,
}

public readonly record struct LaunchSaveResult(LaunchSaveResultKind Kind, string? ErrorMessage)
{
    public bool IsSuccess => Kind == LaunchSaveResultKind.SaveSucceeded;
}

/// <summary>Coordinator port for everything LaunchOrchestrator needs from the main VM.</summary>
public interface ILaunchCoordinatorContext : ICommandExecutionContext
{
    public LauncherSettings GetSettings();
    public new string GetTimestampUtc();
    public void SetDryRunText(string text);
    public void ApplyStatus(LauncherStatusContent status);
    public void DiscardDragReorderPreview();
    public bool PersistSettings(Func<LauncherSettings, LauncherSettings> mutator, LauncherStatusContent? successStatus);
    public IEnumerable<LauncherEntryViewModel> GetEntries();
    public LaunchSaveResult SaveConfig(LaunchSaveResultKind kind);
    public void RecordRunHistory(IReadOnlyList<LauncherRunHistoryEntry> entries);
}

/// <summary>
/// Owns LauncherLaunchWorkflow, single/batch launch flow, and the
/// RequireConfirmationForLaunches toggle. Coordinator context is injected.
/// 失败半径: Medium — 启动失败不影响存储状态。
/// </summary>
public sealed partial class LaunchOrchestrator : ObservableObject, IDisposable
{
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly IDialogService _dialogService;
    private readonly ILaunchCoordinatorContext _context;
    private bool _presentationReady;
    private CancellationTokenSource? _batchCts;
    private bool _isLaunching;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    public bool IsBatchLaunching => _batchCts is not null;

    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches
        ? LauncherLocalization.Format("launch.confirmOn")
        : LauncherLocalization.Format("launch.confirmOff");

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? LauncherLocalization.Format("launch.confirmTipOn")
        : LauncherLocalization.Format("launch.confirmTipOff");

    /// <summary>CommandTemplates 可写 — CommandManagement 在提交后同步。</summary>
    public IReadOnlyDictionary<string, string>? CommandTemplates
    {
        get => _launchWorkflow.CommandTemplates;
        set => _launchWorkflow.CommandTemplates = value;
    }

    public LaunchOrchestrator(
        LauncherLaunchWorkflow launchWorkflow,
        IDialogService dialogService,
        ILaunchCoordinatorContext context)
    {
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ── single launch ──

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        _context.DiscardDragReorderPreview();
        var entry = item.SnapshotEntry();
        var entryName = EntryName.Normalize(entry.Name);
        try
        {
            var plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            _context.SetDryRunText(plan.CommandLine);
            _context.ApplyStatus(LauncherSingleLaunchStatus.Preview(entryName, plan.RequiresConfirmation));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "ShowDryRun");
            if (entryName.Length == 0)
            {
                _context.SetDryRunText(LauncherSingleLaunchReceiptText.PreviewFailed(ex));
                _context.ApplyStatus(LauncherSingleLaunchStatus.PreviewFailed(ex));
                return;
            }

            _context.SetDryRunText(LauncherSingleLaunchReceiptText.PreviewFailed(entryName, ex));
            _context.ApplyStatus(LauncherSingleLaunchStatus.PreviewFailed(entryName));
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        if (_isLaunching) return;
        _isLaunching = true;
        try
        {
            _context.DiscardDragReorderPreview();
            var entry = item.SnapshotEntry();
            var entryName = EntryName.Normalize(entry.Name);
            LaunchPlan? plan = null;
            try
            {
                plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
                var confirmation = new LauncherSingleLaunchConfirmation(entry, plan);
                if (!await ConfirmSingleLaunchIfRequiredAsync(confirmation, entry.Id.Value).ConfigureAwait(true))
                    return;

                _launchWorkflow.Start(plan);
                _context.SetDryRunText(LauncherSingleLaunchReceiptText.Requested(entryName, plan));
                var requestedTimestamp = _context.GetTimestampUtc();
                item.RecordLaunchFromOwner(requestedTimestamp);
                RecordRunHistory([CreateHistory(entry.Id.Value, entryName, requestedTimestamp, LauncherRunSource.Individual, LauncherRunStatus.Requested)]);
                if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedAfterLaunch).IsSuccess)
                    return;
                _context.ApplyStatus(LauncherSingleLaunchStatus.Requested(entryName));
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "Launch");
                RecordRunHistory([CreateHistory(entry.Id.Value, entryName, _context.GetTimestampUtc(), LauncherRunSource.Individual, LauncherRunStatus.Failed)]);
                if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedBeforeLaunch).IsSuccess)
                    return;
                if (entryName.Length == 0)
                {
                    _context.SetDryRunText(LauncherSingleLaunchReceiptText.Failed(ex));
                    _context.ApplyStatus(LauncherSingleLaunchStatus.Failed(ex));
                    return;
                }

                _context.SetDryRunText(plan is null
                    ? LauncherSingleLaunchReceiptText.Failed(entryName, ex)
                    : LauncherSingleLaunchReceiptText.Failed(entryName, plan, ex));
                _context.ApplyStatus(LauncherSingleLaunchStatus.Failed(entryName, ex));
            }
        }
        finally
        {
            _isLaunching = false;
        }
    }

    private async Task<bool> ConfirmSingleLaunchIfRequiredAsync(
        LauncherSingleLaunchConfirmation confirmation,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var entryName = confirmation.EntryName;
        var plan = confirmation.Plan;
        if (!plan.RequiresConfirmation)
            return true;

        bool accepted;
        try
        {
            accepted = await _dialogService.ConfirmLaunchAsync(confirmation).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "LaunchConfirmation");
            RecordRunHistory([CreateHistory(entryId, entryName, _context.GetTimestampUtc(), LauncherRunSource.Individual, LauncherRunStatus.Failed)]);
            if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedBeforeLaunch).IsSuccess)
                return false;
            _context.SetDryRunText(LauncherSingleLaunchReceiptText.ConfirmationFailed(entryName, plan, ex));
            _context.ApplyStatus(LauncherSingleLaunchStatus.Failed(entryName, ex));
            return false;
        }

        if (accepted)
            return true;

        RecordRunHistory([CreateHistory(entryId, entryName, _context.GetTimestampUtc(), LauncherRunSource.Individual, LauncherRunStatus.Canceled)]);
        if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedBeforeLaunch).IsSuccess)
            return false;
        _context.SetDryRunText(LauncherSingleLaunchReceiptText.Canceled(entryName, plan));
        _context.ApplyStatus(LauncherSingleLaunchStatus.Canceled(entryName));
        return false;
    }

    // ── batch launch ──

    public void CancelBatch() => _batchCts?.Cancel();

    internal async Task LaunchBatchAsync(
        IReadOnlyList<LauncherEntryViewModel> entries,
        string confirmationTargetLabel,
        LauncherStatusContent emptyStatus,
        Func<int, LauncherStatusContent> canceledStatus,
        Func<Exception, LauncherStatusContent> failedStatus,
        Func<LauncherBatchLaunchReport, LauncherStatusContent> completedStatus,
        LauncherRunSource source = LauncherRunSource.Selection)
    {
        if (entries.Count == 0)
        {
            _context.ApplyStatus(emptyStatus);
            return;
        }

        if (IsBatchLaunching) return;

        _context.DiscardDragReorderPreview();
        _batchCts?.Dispose();
        _batchCts = new CancellationTokenSource();
        LauncherBatchLaunchPreflight preflight = null!;
        LauncherBatchLaunchReport? completedReport = null;
        try
        {
            OnPropertyChanged(nameof(IsBatchLaunching));
            var selectedEntries = entries.Select(ToLaunchEntry).ToList();
            preflight = _launchWorkflow.PreflightBatch(
                selectedEntries,
                RequireConfirmationForLaunches);
            if (!await ConfirmBatchLaunchIfRequiredAsync(
                    preflight.Items,
                    preflight.Report,
                    confirmationTargetLabel,
                    canceledStatus,
                    failedStatus,
                    source).ConfigureAwait(true))
                return;

            var report = _launchWorkflow.StartBatch(preflight, _batchCts.Token);
            completedReport = report;
            var timestamp = _context.GetTimestampUtc();
            var startedCount = report.RequestedCount;
            var preflightItems = preflight.Items;
            var entryById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
            foreach (var e in _context.GetEntries())
            {
                if (!entryById.TryAdd(e.Id, e))
                {
                    ErrorLog.Write(
                        new InvalidDataException($"Duplicate entry ID in batch launch dictionary: {e.Id}"),
                        "LaunchBatch");
                }
            }
            var failureIndexSet = new HashSet<int>(report.FailureIndices);
            for (var i = 0; i < preflightItems.Count; i++)
            {
                if (failureIndexSet.Contains(i))
                    continue;
                var preflightItem = preflightItems[i];
                entryById.TryGetValue(preflightItem.EntryId, out var entryVm);
                entryVm?.RecordLaunchFromOwner(timestamp);
            }
            // Save AFTER timestamps are recorded so launch history is persisted.
            _context.SetDryRunText(LauncherBatchLaunchReceiptText.Format(preflight, report));
            RecordRunHistory(BuildBatchHistory(preflight.Items, report, timestamp, source));
            if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedAfterLaunch).IsSuccess)
                return;
            _context.ApplyStatus(completedStatus(report));
        }
        catch (OperationCanceledException)
        {
            if (completedReport is not null)
            {
                RecordRunHistory(BuildBatchHistory(preflight.Items, completedReport, _context.GetTimestampUtc(), source));
                if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedAfterLaunch).IsSuccess)
                    return;
            }
            _context.SetDryRunText(LauncherBatchLaunchReceiptText.Canceled(preflight?.Items ?? []));
            _context.ApplyStatus(LauncherRefreshStatus.CanceledBatch);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "BatchLaunch");
            _context.SetDryRunText(LauncherBatchLaunchReceiptText.Failed(ex));
            _context.ApplyStatus(failedStatus(ex));
        }
        finally
        {
            _batchCts?.Dispose();
            _batchCts = null;
            OnPropertyChanged(nameof(IsBatchLaunching));
        }
    }

    private async Task<bool> ConfirmBatchLaunchIfRequiredAsync(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report,
        string confirmationTargetLabel,
        Func<int, LauncherStatusContent> canceledStatus,
        Func<Exception, LauncherStatusContent> failedStatus,
        LauncherRunSource source)
    {
        if (!RequireConfirmationForLaunches && report.FailedCount == 0 && !items.Any(i => i.Plan.RequiresConfirmation))
            return true;

        bool accepted;
        var confirmation = new LauncherBatchLaunchConfirmation(
            items,
            confirmationTargetLabel,
            report.FailedCount,
            report.FailureMessages);
        try
        {
            accepted = await _dialogService.ConfirmBatchLaunchAsync(confirmation).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "BatchLaunchConfirmation");
            RecordRunHistory(BuildDecisionHistory(items, report, source, LauncherRunStatus.Failed, _context.GetTimestampUtc()));
            if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedBeforeLaunch).IsSuccess)
                return false;
            _context.SetDryRunText(LauncherBatchLaunchReceiptText.ConfirmationFailed(confirmation.Items, ex));
            _context.ApplyStatus(failedStatus(ex));
            return false;
        }

        if (accepted)
            return true;

        RecordRunHistory(BuildDecisionHistory(items, report, source, LauncherRunStatus.Canceled, _context.GetTimestampUtc()));
        if (!_context.SaveConfig(LaunchSaveResultKind.SaveFailedBeforeLaunch).IsSuccess)
            return false;
        _context.SetDryRunText(LauncherBatchLaunchReceiptText.Canceled(confirmation.Items));
        _context.ApplyStatus(canceledStatus(confirmation.Items.Count));
        return false;
    }

    private void RecordRunHistory(IEnumerable<LauncherRunHistoryEntry> entries)
    {
        try
        {
            _context.RecordRunHistory(entries.ToList());
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "RunHistory");
        }
    }

    private static LauncherRunHistoryEntry CreateHistory(
        string entryId,
        string entryName,
        string timestampUtc,
        LauncherRunSource source,
        LauncherRunStatus status) =>
        LauncherRunHistoryEntry.Create(entryId, entryName, timestampUtc, source, status);

    private static ImmutableList<LauncherRunHistoryEntry> BuildBatchHistory(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report,
        string timestampUtc,
        LauncherRunSource source)
    {
        var entries = new List<LauncherRunHistoryEntry>();
        var failures = new HashSet<int>(report.FailureIndices);
        var canceledStart = items.Count - report.SkippedCount;
        for (var index = 0; index < items.Count; index++)
        {
            var status = failures.Contains(index)
                ? LauncherRunStatus.Failed
                : report.SkippedCount > 0 && index >= canceledStart
                    ? LauncherRunStatus.Skipped
                    : LauncherRunStatus.Requested;
            entries.Add(CreateHistory(
                items[index].EntryId,
                items[index].EntryName,
                timestampUtc,
                source,
                status));
        }

        for (var index = 0; index < report.FailureEntryIds.Count; index++)
        {
            var entryId = report.FailureEntryIds[index];
            var entryName = index < report.FailureEntryNames.Count
                ? report.FailureEntryNames[index]
                : entryId;
            entries.Add(CreateHistory(entryId, entryName, timestampUtc, source, LauncherRunStatus.Failed));
        }

        return entries.ToImmutableList();
    }

    private static ImmutableList<LauncherRunHistoryEntry> BuildDecisionHistory(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report,
        LauncherRunSource source,
        LauncherRunStatus status,
        string timestampUtc)
    {
        var entries = items.Select(item =>
            CreateHistory(item.EntryId, item.EntryName, timestampUtc, source, status));
        var failedPreflight = Enumerable.Range(0, report.FailureEntryIds.Count)
            .Select(index => CreateHistory(
                report.FailureEntryIds[index],
                index < report.FailureEntryNames.Count
                    ? report.FailureEntryNames[index]
                    : report.FailureEntryIds[index],
                timestampUtc,
                source,
                LauncherRunStatus.Failed));
        return entries.Concat(failedPreflight).ToImmutableList();
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    internal void MarkContextReady() => _presentationReady = true;

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        if (!_presentationReady)
            return;

        RefreshLaunchConfirmationPresentation();
        if (_context.PersistSettings(
                s => s with { RequireConfirmationForLaunches = value },
                LauncherSettingsStatus.LaunchConfirmationSaved(value)))
        {
            // Success
        }
        else
        {
            // Rollback — restore property to stored value.
            _requireConfirmationForLaunches = _context.GetSettings().RequireConfirmationForLaunches;
            OnPropertyChanged(nameof(RequireConfirmationForLaunches));
            OnPropertyChanged(nameof(LaunchConfirmationToggleText));
            OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        }
    }

    /// <summary>Build preflight plan for preview (used by preset preview).</summary>
    internal LauncherBatchLaunchPreflight PreflightBatch(IEnumerable<LaunchEntry> entries) =>
        _launchWorkflow.PreflightBatch(entries, RequireConfirmationForLaunches);

    internal void RefreshLaunchConfirmationPresentation()
    {
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
    }

    /// <summary>释放 _batchCts 防止 Timer 泄漏。</summary>
    public void Dispose()
    {
        _batchCts?.Dispose();
        _batchCts = null;
    }
}
