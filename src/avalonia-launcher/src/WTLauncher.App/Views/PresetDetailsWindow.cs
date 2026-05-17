using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed class PresetDetailsWindow : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBlock _errorText;

    public PresetDetailsWindow(IReadOnlyList<string> selectedEntryIds)
    {
        Title = "保存为启动组";
        Width = 640;
        Height = 380;
        MinWidth = 560;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        _nameBox = new TextBox
        {
            Text = "新启动组",
            FontSize = 14,
            Padding = new Thickness(10, 8),
        };

        var descriptionBox = new TextBox
        {
            PlaceholderText = "可选描述，帮助未来回看为什么要保存这一组",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 96,
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
            Text = $"将当前 {selectedEntryIds.Count} 个勾选项保存为可复用启动组，后续可更新或删除。",
            Foreground = Brush.Parse("#475569"),
            TextWrapping = TextWrapping.Wrap,
        };

        var cancel = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 8),
        };
        cancel.Click += (_, _) => Close((PresetDraft?)null);

        var save = new Button
        {
            Content = "保存启动组",
            Padding = new Thickness(18, 8),
            Background = Brush.Parse("#2563EB"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
        };
        save.Click += (_, _) =>
        {
            var name = (_nameBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                _errorText.Text = "请输入启动组名称。";
                _errorText.IsVisible = true;
                _nameBox.Focus();
                return;
            }

            Close(new PresetDraft(name, (descriptionBox.Text ?? string.Empty).Trim()));
        };

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 14, 0, 0),
            Children = { cancel, save },
        };

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
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
                    Place(summary, 1),
                    Place(new TextBlock
                    {
                        Text = "名称",
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#334155"),
                    }, 2),
                    Place(_nameBox, 3),
                    Place(new TextBlock
                    {
                        Text = "描述",
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#334155"),
                    }, 4),
                    Place(descriptionBox, 5),
                    Place(_errorText, 6),
                    Place(footer, 7),
                },
            },
        };

        Opened += (_, _) =>
        {
            _nameBox.SelectAll();
            _nameBox.Focus();
        };
    }

    private static T Place<T>(T control, int row)
        where T : Control
    {
        Grid.SetRow(control, row);
        return control;
    }
}
