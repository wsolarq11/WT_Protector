using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class UpdateCheckWindow : Window
{
    private readonly UpdateCheckViewModel _viewModel;

    public UpdateCheckWindow()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public UpdateCheckWindow(UpdateCheckViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.CheckAsync();
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "UpdateCheck");
        }
    }
}
