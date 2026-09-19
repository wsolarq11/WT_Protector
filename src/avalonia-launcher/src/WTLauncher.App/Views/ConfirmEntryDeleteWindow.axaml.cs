using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmEntryDeleteWindow : Window
{
    public ConfirmEntryDeleteWindow()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public ConfirmEntryDeleteWindow(LauncherEntryDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        InitializeComponent();
        ApplyLocalizedChrome();
        BodyText.Text = LauncherLocalization.Format("confirmEntryDelete.body", target.EntryName);
        Opened += (_, _) => CancelButton.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        Title = LauncherLocalization.Format("confirmEntryDelete.title");
        HeadingText.Text = LauncherLocalization.Format("confirmEntryDelete.heading");
        CancelButton.Content = LauncherLocalization.Format("confirmEntryDelete.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("confirmEntryDelete.cancelAutomation"));
        DeleteButton.Content = LauncherLocalization.Format("confirmEntryDelete.confirm");
        AutomationProperties.SetName(DeleteButton, LauncherLocalization.Format("confirmEntryDelete.confirmAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnDeleteClicked(object? sender, RoutedEventArgs e) => Close(true);
}
