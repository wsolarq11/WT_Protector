using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Attach(MainWindowViewModel viewModel)
    {
        viewModel.ConfirmLaunchAsync = ConfirmLaunchAsync;
    }

    private async Task<bool> ConfirmLaunchAsync(LaunchEntry entry, LaunchPlan plan)
    {
        var dialog = new ConfirmLaunchWindow(entry, plan);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }
}
