using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    private Func<LauncherSingleLaunchConfirmation, Task<bool>> _confirmLaunchAsync =
        static _ => throw new InvalidOperationException("ConfirmLaunchAsync is not bound by the View.");

    public Func<LauncherSingleLaunchConfirmation, Task<bool>> ConfirmLaunchAsync
    {
        get => _confirmLaunchAsync;
        set => _confirmLaunchAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            RefreshLaunchConfirmationPresentation();

            if (TryPersistSettingsChange(
                s => s with { RequireConfirmationForLaunches = value },
                LauncherSettingsStatus.LaunchConfirmationSaved(value)))
            {
                // Success — status and presentation already applied.
            }
            else
            {
                _requireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                RefreshLaunchConfirmationPresentation();
            }
        }
        finally
        {
            _isHandlingSettingsChange = false;
        }
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var entry = item.SnapshotEntry();
        var entryName = LauncherEntryText.Normalize(entry.Name);
        try
        {
            var plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            DryRunText = plan.CommandLine;
            StatusText = LauncherSingleLaunchStatus.Preview(entryName, plan.RequiresConfirmation);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "ShowDryRun");
            if (entryName.Length == 0)
            {
                DryRunText = LauncherSingleLaunchReceiptText.PreviewFailed(ex);
                StatusText = LauncherSingleLaunchStatus.PreviewFailed(ex);
                return;
            }

            DryRunText = LauncherSingleLaunchReceiptText.PreviewFailed(entryName, ex);
            StatusText = LauncherSingleLaunchStatus.PreviewFailed(entryName);
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var entry = item.SnapshotEntry();
        var entryName = LauncherEntryText.Normalize(entry.Name);
        LaunchPlan? plan = null;
        try
        {
            plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            var confirmation = new LauncherSingleLaunchConfirmation(entry, plan);
            if (!await ConfirmSingleLaunchIfRequiredAsync(confirmation).ConfigureAwait(true))
                return;

            _launchWorkflow.Start(plan);
            item.RecordLaunchFromOwner(CurrentTimestampUtc());
            var saveOk = SaveConfig();
            DryRunText = LauncherSingleLaunchReceiptText.Requested(entryName, plan);
            if (saveOk)
                StatusText = LauncherSingleLaunchStatus.Requested(entryName);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Launch");
            if (entryName.Length == 0)
            {
                DryRunText = LauncherSingleLaunchReceiptText.Failed(ex);
                StatusText = LauncherSingleLaunchStatus.Failed(ex);
                return;
            }

            DryRunText = plan is null
                ? LauncherSingleLaunchReceiptText.Failed(entryName, ex)
                : LauncherSingleLaunchReceiptText.Failed(entryName, plan, ex);
            StatusText = LauncherSingleLaunchStatus.Failed(entryName, ex);
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
            accepted = await ConfirmLaunchAsync(confirmation).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "LaunchConfirmation");
            DryRunText = LauncherSingleLaunchReceiptText.ConfirmationFailed(entryName, plan, ex);
            StatusText = LauncherSingleLaunchStatus.Failed(entryName, ex);
            return false;
        }

        if (accepted)
            return true;

        DryRunText = LauncherSingleLaunchReceiptText.Canceled(entryName, plan);
        StatusText = LauncherSingleLaunchStatus.Canceled(entryName);
        return false;
    }

    private void RefreshLaunchConfirmationPresentation()
    {
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        LauncherEntryLayoutPresentation.Refresh(_entries);
        ApplyFilter();
    }

}
