using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WTLauncher.App.Views;

public sealed partial class LauncherControlBar : UserControl
{
    public LauncherControlBar()
    {
        InitializeComponent();
    }

    public AutoCompleteBox SearchBox => SearchTextBox;

    public ComboBox DisplayModeBox => DisplayModeComboBox;

    private static void OnOverflowMenuClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { ContextMenu: { } menu } control)
        {
            menu.Open(control);
            e.Handled = true;
        }
    }
}
