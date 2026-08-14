using Avalonia.Controls;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class RunHistoryWindow : Window
{
    public RunHistoryWindow()
    {
        InitializeComponent();
    }

    public RunHistoryWindow(RunHistoryViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        DataContext = viewModel;
        InitializeComponent();
    }
}
