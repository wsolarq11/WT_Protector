using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed class ConfirmEntryDeleteWindow : Window
{
    public ConfirmEntryDeleteWindow(LauncherEntryViewModel entry)
    {
        Title = "删除按钮";
        Width = 520;
        Height = 260;
        MinWidth = 480;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var cancel = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 8),
        };
        cancel.Click += (_, _) => Close(false);

        var delete = new Button
        {
            Content = "删除按钮",
            Padding = new Thickness(18, 8),
            Background = Brush.Parse("#DC2626"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
        };
        delete.Click += (_, _) => Close(true);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "确认删除启动按钮？",
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#0F172A"),
                    },
                    new TextBlock
                    {
                        Text = $"“{entry.Name}” 会从 config\\config.json 删除，并从所有启动组里移除引用。",
                        Foreground = Brush.Parse("#475569"),
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Margin = new Thickness(0, 24, 0, 0),
                        Children = { cancel, delete },
                    },
                },
            },
        };
    }
}
