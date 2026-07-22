using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmLaunchWindow : Window
{
    public ConfirmLaunchWindow()
    {
        InitializeComponent();
    }

    public ConfirmLaunchWindow(LauncherSingleLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        InitializeComponent();
        TitleText.Text = $"启动：{confirmation.EntryName}";
        CommandText.Text = confirmation.Plan.CommandLine;
        Opened += (_, _) => LaunchButton.Focus();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnLaunchClicked(object? sender, RoutedEventArgs e) => Close(true);
}
