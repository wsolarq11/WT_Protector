using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmPresetDeleteWindow : Window
{
    public ConfirmPresetDeleteWindow()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public ConfirmPresetDeleteWindow(LauncherPresetDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        InitializeComponent();
        ApplyLocalizedChrome();
        BodyText.Text = LauncherLocalization.Format("confirmPresetDelete.body", target.PresetName);
        SubtleText.Text = LauncherLocalization.Format("confirmPresetDelete.subtle", target.EntryCount);
        Opened += (_, _) => CancelButton.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        Title = LauncherLocalization.Format("confirmPresetDelete.title");
        HeadingText.Text = LauncherLocalization.Format("confirmPresetDelete.heading");
        CancelButton.Content = LauncherLocalization.Format("confirmPresetDelete.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("confirmPresetDelete.cancelAutomation"));
        DeleteButton.Content = LauncherLocalization.Format("confirmPresetDelete.confirm");
        AutomationProperties.SetName(DeleteButton, LauncherLocalization.Format("confirmPresetDelete.confirmAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnDeleteClicked(object? sender, RoutedEventArgs e) => Close(true);
}
