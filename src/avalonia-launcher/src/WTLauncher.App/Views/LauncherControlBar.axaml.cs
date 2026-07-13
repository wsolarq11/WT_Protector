using Avalonia.Controls;

namespace WTLauncher.App.Views;

public sealed partial class LauncherControlBar : UserControl
{
    public LauncherControlBar()
    {
        InitializeComponent();
    }

    public AutoCompleteBox SearchBox => SearchTextBox;

    public ComboBox DisplayModeBox => DisplayModeComboBox;
}
