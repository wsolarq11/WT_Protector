using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmBatchLaunchWindow : Window
{
    public ConfirmBatchLaunchWindow()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public ConfirmBatchLaunchWindow(LauncherBatchLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var hasFailures = confirmation.FailedCount > 0;
        InitializeComponent();
        ApplyLocalizedChrome();

        Title = hasFailures
            ? LauncherLocalization.Format("confirmBatch.titleWithFailures")
            : LauncherLocalization.Format("confirmBatch.title");
        Height = hasFailures ? 640 : 520;
        MinHeight = hasFailures ? 460 : 340;

        TitleText.Text = hasFailures
            ? LauncherLocalization.Format("confirmBatch.titleTextWithFailures", confirmation.TargetLabel, confirmation.FailedCount)
            : LauncherLocalization.Format("confirmBatch.titleText", confirmation.TargetLabel);

        var items = confirmation.Items;
        MessageText.Text = hasFailures
            ? LauncherLocalization.Format("confirmBatch.messageWithFailures", confirmation.FailedCount, items.Count, confirmation.TargetLabel)
            : LauncherLocalization.Format("confirmBatch.message", items.Count, confirmation.TargetLabel);

        PlansText.Text = LauncherBatchLaunchPlanText.Format(items);

        LaunchButton.Content = hasFailures
            ? LauncherLocalization.Format("confirmBatch.launchValid")
            : LauncherLocalization.Format("confirmBatch.confirm");
        LaunchButton.MinWidth = hasFailures ? 140 : 116;
        AutomationProperties.SetName(LaunchButton, LauncherLocalization.Format("confirmBatch.confirmAutomation"));

        if (hasFailures)
        {
            FailureSection.IsVisible = true;
            var failureLines = string.Join(
                Environment.NewLine,
                confirmation.FailureMessages.Select(f => $"- {f}"));
            FailureText.Text = failureLines;
        }

        Opened += (_, _) => LaunchButton.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        FailureHeader.Text = LauncherLocalization.Format("confirmBatch.failureHeader");
        CancelButton.Content = LauncherLocalization.Format("confirmBatch.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("confirmBatch.cancelAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnLaunchClicked(object? sender, RoutedEventArgs e) => Close(true);
}
