using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class QuickLaunchWindow : Window
{
    private readonly QuickLaunchViewModel? _viewModel;

    public QuickLaunchWindow()
    {
        InitializeComponent();
    }

    public QuickLaunchWindow(QuickLaunchViewModel viewModel)
        : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        AddHandler(KeyDownEvent, OnQuickLaunchKeyDown, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => Hide();
    }

    private void OnQuickLaunchKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Enter:
                _viewModel.LaunchSelectedCommand.Execute(null);
                Hide();
                e.Handled = true;
                break;
            case Key.Down:
                _viewModel.MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                _viewModel.MoveSelection(-1);
                e.Handled = true;
                break;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        QueryBox.Focus();
        QueryBox.SelectAll();
    }

}
