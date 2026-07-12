using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Single-entry launch (DryRun + Launch) and RequireConfirmationForLaunches toggle.
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-stage2.

public sealed partial class MainWindowViewModel
{
    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    // ConfirmLaunchAsync is delegated through IDialogService.

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
        var entryName = EntryName.Normalize(entry.Name);
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
        if (_isLaunching) return;
        _isLaunching = true;
        try
        {
            DiscardDragReorderPreview();
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
                DryRunText = LauncherSingleLaunchReceiptText.Requested(entryName, plan);
                item.RecordLaunchFromOwner(CurrentTimestampUtc());
                if (!SaveConfig())
                    return;
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
