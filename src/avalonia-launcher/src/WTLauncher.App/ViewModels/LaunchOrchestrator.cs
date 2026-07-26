using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Owns LauncherLaunchWorkflow, single/batch launch flow, and the
/// RequireConfirmationForLaunches toggle. Bridge delegates set by MWVM.
/// 失败半径: Medium — 启动失败不影响存储状态。
/// </summary>
// ponytail: CA1001 wants IDisposable because of _batchCts.
// _batchCts is deterministically disposed in LaunchBatchAsync's finally block.
#pragma warning disable CA1001
public sealed partial class LaunchOrchestrator : ObservableObject
{
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _batchCts;
    private bool _isLaunching;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    public bool IsBatchLaunching => _batchCts is not null;

    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    // ── bridge delegates (set by MWVM) ──
    internal Func<LauncherSettings> SettingsGetter = null!;
    internal Func<string> TimestampGetter = null!;
    internal Action<string> DryRunTextSetter = null!;
    internal Action<LauncherStatusContent> StatusApplier = null!;
    internal Action FilterApplier = null!;
    internal Action ReorderDiscarder = null!;
    internal Func<Func<LauncherSettings, LauncherSettings>, LauncherStatusContent?, bool> SettingsPersister = null!;
    internal Func<IEnumerable<LauncherEntryViewModel>> EntryGetter = null!;
    internal Func<bool> SaveConfig = null!;

    /// <summary>CommandTemplates 可写 — CommandManagement 在提交后同步。</summary>
    public IReadOnlyDictionary<string, string>? CommandTemplates
    {
        get => _launchWorkflow.CommandTemplates;
        set => _launchWorkflow.CommandTemplates = value;
    }

    public LaunchOrchestrator(LauncherLaunchWorkflow launchWorkflow, IDialogService dialogService)
    {
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    // ── single launch ──

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        ReorderDiscarder();
        var entry = item.SnapshotEntry();
        var entryName = EntryName.Normalize(entry.Name);
        try
        {
            var plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            DryRunTextSetter(plan.CommandLine);
            StatusApplier(LauncherSingleLaunchStatus.Preview(entryName, plan.RequiresConfirmation));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "ShowDryRun");
            if (entryName.Length == 0)
            {
                DryRunTextSetter(LauncherSingleLaunchReceiptText.PreviewFailed(ex));
                StatusApplier(LauncherSingleLaunchStatus.PreviewFailed(ex));
                return;
            }

            DryRunTextSetter(LauncherSingleLaunchReceiptText.PreviewFailed(entryName, ex));
            StatusApplier(LauncherSingleLaunchStatus.PreviewFailed(entryName));
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        if (_isLaunching) return;
        _isLaunching = true;
        try
        {
            ReorderDiscarder();
            var entry = item.SnapshotEntry();
            var entryName = EntryName.Normalize(entry.Name);
            LaunchPlan? plan = null;
            try
            {
                plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
                var confirmation = new LauncherSingleLaunchConfirmation(entry, plan);
                if (!await ConfirmSingleLaunchIfRequiredAsync(confirmation).ConfigureAwait(true))
                    return;

                _launchWorkflow.Start(plan);
                DryRunTextSetter(LauncherSingleLaunchReceiptText.Requested(entryName, plan));
                item.RecordLaunchFromOwner(TimestampGetter());
                if (!SaveConfig())
                    return;
                StatusApplier(LauncherSingleLaunchStatus.Requested(entryName));
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "Launch");
                if (entryName.Length == 0)
                {
                    DryRunTextSetter(LauncherSingleLaunchReceiptText.Failed(ex));
                    StatusApplier(LauncherSingleLaunchStatus.Failed(ex));
                    return;
                }

                DryRunTextSetter(plan is null
                    ? LauncherSingleLaunchReceiptText.Failed(entryName, ex)
                    : LauncherSingleLaunchReceiptText.Failed(entryName, plan, ex));
                StatusApplier(LauncherSingleLaunchStatus.Failed(entryName, ex));
            }
        }
        finally
        {
            _isLaunching = false;
        }
    }

    private async Task<bool> ConfirmSingleLaunchIfRequiredAsync(LauncherSingleLaunchConfirmation confirmation)
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
            DryRunTextSetter(LauncherSingleLaunchReceiptText.ConfirmationFailed(entryName, plan, ex));
            StatusApplier(LauncherSingleLaunchStatus.Failed(entryName, ex));
            return false;
        }

        if (accepted)
            return true;

        DryRunTextSetter(LauncherSingleLaunchReceiptText.Canceled(entryName, plan));
        StatusApplier(LauncherSingleLaunchStatus.Canceled(entryName));
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
        Func<LauncherBatchLaunchReport, LauncherStatusContent> completedStatus)
    {
        if (entries.Count == 0)
        {
            StatusApplier(emptyStatus);
            return;
        }

        if (IsBatchLaunching) return;

        ReorderDiscarder();
        _batchCts?.Dispose();
        _batchCts = new CancellationTokenSource();
        LauncherBatchLaunchPreflight preflight = null!;
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
                    failedStatus).ConfigureAwait(true))
                return;

            var report = _launchWorkflow.StartBatch(preflight, _batchCts.Token);
            var timestamp = TimestampGetter();
            var startedCount = report.RequestedCount;
            var preflightItems = preflight.Items;
            var entryById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
            foreach (var e in EntryGetter())
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
            DryRunTextSetter(LauncherBatchLaunchReceiptText.Format(preflight, report));
            if (startedCount > 0 && !SaveConfig())
                return;
            StatusApplier(completedStatus(report));
        }
        catch (OperationCanceledException)
        {
            DryRunTextSetter(LauncherBatchLaunchReceiptText.Canceled(preflight?.Items ?? []));
            StatusApplier(LauncherRefreshStatus.CanceledBatch);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "BatchLaunch");
            DryRunTextSetter(LauncherBatchLaunchReceiptText.Failed(ex));
            StatusApplier(failedStatus(ex));
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
        Func<Exception, LauncherStatusContent> failedStatus)
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
            DryRunTextSetter(LauncherBatchLaunchReceiptText.ConfirmationFailed(confirmation.Items, ex));
            StatusApplier(failedStatus(ex));
            return false;
        }

        if (accepted)
            return true;

        DryRunTextSetter(LauncherBatchLaunchReceiptText.Canceled(confirmation.Items));
        StatusApplier(canceledStatus(confirmation.Items.Count));
        return false;
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        // Skip persistence when delegates aren't wired yet (construction).
        if (SettingsPersister is null) return;

        RefreshLaunchConfirmationPresentation();
        if (SettingsPersister(
                s => s with { RequireConfirmationForLaunches = value },
                LauncherSettingsStatus.LaunchConfirmationSaved(value)))
        {
            // Success
        }
        else
        {
            // Rollback — restore property to stored value.
            _requireConfirmationForLaunches = SettingsGetter().RequireConfirmationForLaunches;
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
        FilterApplier();
    }
}
