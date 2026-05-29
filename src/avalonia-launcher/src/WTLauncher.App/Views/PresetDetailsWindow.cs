using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WTLauncher.App.Views;

public sealed class PresetDetailsWindow : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBlock _errorText;

    public PresetDetailsWindow(IReadOnlyList<string> selectedEntryIds)
    {
        Title = "保存为启动组";
        Width = 560;
        Height = 260;
        MinWidth = 480;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        _nameBox = new TextBox
        {
            Text = "新启动组",
            FontSize = 14,
            Padding = new Thickness(10, 8),
        };

        _errorText = new TextBlock
        {
            Foreground = Brushes.IndianRed,
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };

        var summary = new TextBlock
        {
            Text = $"将当前 {selectedEntryIds.Count} 个勾选项保存为可复用启动组。",
            Foreground = Brush.Parse("#475569"),
            TextWrapping = TextWrapping.Wrap,
        };

        var cancel = LauncherDialogButtons.Cancel();
        cancel.Click += (_, _) => Close((string?)null);

        var save = LauncherDialogButtons.Primary("保存启动组");
        save.Click += (_, _) => Submit();

        var footer = LauncherDialogButtons.Footer(14, cancel, save);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
                RowSpacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "保存当前勾选为新启动组",
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#0F172A"),
                    },
                    summary.WithGridRow(1),
                    new TextBlock
                    {
                        Text = "名称",
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#334155"),
                    }.WithGridRow(2),
                    _nameBox.WithGridRow(3),
                    _errorText.WithGridRow(4),
                    footer.WithGridRow(5),
                },
            },
        };

        Opened += (_, _) =>
        {
            _nameBox.SelectAll();
            _nameBox.Focus();
        };
    }

    private void Submit()
    {
        var name = (_nameBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _errorText.Text = "请输入启动组名称。";
            _errorText.IsVisible = true;
            _nameBox.Focus();
            return;
        }

        Close(name);
    }
}
