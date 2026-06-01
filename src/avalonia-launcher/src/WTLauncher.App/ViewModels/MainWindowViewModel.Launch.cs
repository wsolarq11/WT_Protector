using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    public Func<LaunchEntry, LaunchPlan, Task<bool>> ConfirmLaunchAsync { get; set; } =
        static (_, _) => Task.FromResult(false);

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        _settings.RequireConfirmationForLaunches = value;
        RefreshLaunchConfirmationPresentation();
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
            OnPropertyChanged(nameof(RequireConfirmationForLaunches));
            RefreshLaunchConfirmationPresentation();
            ApplyStatusText(saveFailure);
            return;
        }

        StatusText = LauncherSettingsStatus.LaunchConfirmationSaved(value);
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var entry = SnapshotLaunchEntry(item.Entry);
        var entryName = LauncherEntryText.Normalize(entry.Name);
        try
        {
            var plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            DryRunText = plan.CommandLine;
            StatusText = LauncherSingleLaunchStatus.Preview(entryName, plan.RequiresConfirmation);
        }
        catch (Exception ex)
        {
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
        var entry = SnapshotLaunchEntry(item.Entry);
        var entryName = LauncherEntryText.Normalize(entry.Name);
        LaunchPlan? plan = null;
        try
        {
            plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            if (!await ConfirmSingleLaunchIfRequiredAsync(entry, entryName, plan).ConfigureAwait(true))
                return;

            _launchWorkflow.Start(plan);
            DryRunText = LauncherSingleLaunchReceiptText.Requested(entryName, plan);
            StatusText = LauncherSingleLaunchStatus.Requested(entryName);
        }
        catch (Exception ex)
        {
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

    private async Task<bool> ConfirmSingleLaunchIfRequiredAsync(LaunchEntry entry, string entryName, LaunchPlan plan)
    {
        if (!plan.RequiresConfirmation)
            return true;

        bool accepted;
        try
        {
            accepted = await ConfirmLaunchAsync(entry, plan).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
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
        LauncherEntryLayoutPresentation.Refresh(Entries);
        ApplyFilter();
    }

    private static LaunchEntry SnapshotLaunchEntry(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry with { };
    }
}
