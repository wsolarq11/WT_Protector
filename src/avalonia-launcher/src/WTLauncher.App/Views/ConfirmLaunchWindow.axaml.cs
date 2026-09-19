using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmLaunchWindow : Window
{
    public ConfirmLaunchWindow()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public ConfirmLaunchWindow(LauncherSingleLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        InitializeComponent();
        ApplyLocalizedChrome();
        TitleText.Text = LauncherLocalization.Format("confirmLaunch.titleWithEntry", confirmation.EntryName);
        CommandText.Text = confirmation.Plan.CommandLine;
        Opened += (_, _) => LaunchButton.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        Title = LauncherLocalization.Format("confirmLaunch.title");
        NoteText.Text = LauncherLocalization.Format("confirmLaunch.note");
        CancelButton.Content = LauncherLocalization.Format("confirmLaunch.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("confirmLaunch.cancelAutomation"));
        LaunchButton.Content = LauncherLocalization.Format("confirmLaunch.confirm");
        AutomationProperties.SetName(LaunchButton, LauncherLocalization.Format("confirmLaunch.confirmAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnLaunchClicked(object? sender, RoutedEventArgs e) => Close(true);
}
