using Avalonia.Controls;

namespace WTLauncher.App.Views.Controls;

public sealed partial class FilterToolbar : UserControl
{
    public FilterToolbar()
    {
        InitializeComponent();
    }

    public AutoCompleteBox SearchBox => SearchTextBox;

    public ComboBox DisplayModeBox => DisplayModeComboBox;
}
