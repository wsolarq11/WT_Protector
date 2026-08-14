using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ImportMigrationWindow : Window
{
    private readonly ImportConfigViewModel _viewModel;

    public ImportMigrationWindow()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public ImportMigrationWindow(ImportConfigViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void OnConfirmClicked(object? sender, RoutedEventArgs e) => Close(_viewModel.BuildPlan());
}
