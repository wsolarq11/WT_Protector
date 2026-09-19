using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.Localization;

namespace WTLauncher.App.Views;

public sealed partial class CommandEditDialog : Window
{
    private readonly bool _isTwoField;
    private readonly IReadOnlySet<string> _reservedKeys = null!;

    public CommandEditDialog()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public CommandEditDialog(
        string title,
        string label,
        string initialValue,
        IReadOnlyList<string> templateKeys,
        string? secondLabel = null,
        bool isMultiline = false)
    {
        _isTwoField = secondLabel is not null;
        _reservedKeys = new HashSet<string>(templateKeys, StringComparer.OrdinalIgnoreCase);

        InitializeComponent();
        ApplyLocalizedChrome();

        Title = title;
        TitleText.Text = title;
        Label1Text.Text = label;
        Input1.Text = initialValue;

        if (isMultiline && !_isTwoField)
            Input1.AcceptsReturn = true;

        if (_isTwoField)
        {
            SecondFieldPanel.IsVisible = true;
            Label2Text.Text = secondLabel!;
        }

        Opened += (_, _) => Input1.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        CancelButton.Content = LauncherLocalization.Format("editDialog.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("editDialog.cancelAutomation"));
        OkButton.Content = LauncherLocalization.Format("editDialog.ok");
        AutomationProperties.SetName(OkButton, LauncherLocalization.Format("editDialog.okAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        var v1 = Input1.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(v1))
        {
            ShowError(LauncherLocalization.Format("editDialog.valueRequired"));
            return;
        }

        if (_isTwoField)
        {
            if (_reservedKeys.Contains(v1))
            {
                ShowError(LauncherLocalization.Format("maintenance.keyExists", v1));
                return;
            }
            var v2 = Input2.Text ?? string.Empty;
            Close((v1, v2));
            return;
        }

        Close(v1);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
