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
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.RequireConfirmationForLaunches = value;
        RefreshLaunchConfirmationPresentation();
        SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                RefreshLaunchConfirmationPresentation();
            },
            () => StatusText = LauncherSettingsStatus.LaunchConfirmationSaved(value));
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        try
        {
            var plan = _launchWorkflow.BuildPlan(item.Entry, RequireConfirmationForLaunches);
            DryRunText = plan.CommandLine;
            StatusText = LauncherSingleLaunchStatus.Preview(item.Name, plan.RequiresConfirmation);
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
            StatusText = LauncherSingleLaunchStatus.PreviewFailed(item.Name);
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        string? dryRunText = null;
        try
        {
            var plan = _launchWorkflow.BuildPlan(item.Entry, RequireConfirmationForLaunches);
            dryRunText = plan.CommandLine;
            if (!await ConfirmSingleLaunchIfRequiredAsync(item, plan).ConfigureAwait(true))
                return;

            _launchWorkflow.Start(plan);
            DryRunText = dryRunText;
            StatusText = LauncherSingleLaunchStatus.Started(item.Name);
        }
        catch (Exception ex)
        {
            if (dryRunText is not null)
                DryRunText = dryRunText;
            StatusText = LauncherSingleLaunchStatus.Failed(item.Name, ex);
        }
    }

    private async Task<bool> ConfirmSingleLaunchIfRequiredAsync(LauncherEntryViewModel item, LaunchPlan plan)
    {
        if (!plan.RequiresConfirmation)
            return true;

        var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
        if (accepted)
            return true;

        DryRunText = plan.CommandLine;
        StatusText = LauncherSingleLaunchStatus.Canceled(item.Name);
        return false;
    }

    private void RefreshLaunchConfirmationPresentation()
    {
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        LauncherEntryLayoutPresentation.Refresh(Entries);
        ApplyFilter();
    }
}
