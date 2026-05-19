using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class ConfirmBatchLaunchWindow : Window
{
    public ConfirmBatchLaunchWindow(IReadOnlyList<LaunchEntry> entries)
    {
        Title = "确认一键启动";
        Width = 560;
        Height = 420;
        MinWidth = 460;
        MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var names = string.Join(Environment.NewLine, entries.Select(entry => "• " + entry.Name));
        var message = $"将启动 {entries.Count} 个已勾选项。";

        var cancel = new Button
        {
            Content = "取消",
            MinWidth = 96,
        };
        cancel.Click += (_, _) => Close(false);

        var launch = new Button
        {
            Content = "确认启动",
            MinWidth = 116,
        };
        launch.Click += (_, _) => Close(true);

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto),
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = "一键启动已勾选项？",
                        FontSize = 22,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = message,
                        Margin = new Thickness(0, 12, 0, 14),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    }.WithGridRow(1),
                    new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        BorderBrush = Avalonia.Media.Brushes.LightGray,
                        Padding = new Thickness(12),
                        Child = new ScrollViewer
                        {
                            Content = new TextBlock
                            {
                                Text = names,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            },
                        },
                    }.WithGridRow(2),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 10,
                        Margin = new Thickness(0, 18, 0, 0),
                        Children = { cancel, launch },
                    }.WithGridRow(3),
                },
            },
        };
    }
}

file static class GridExtensions
{
    public static T WithGridRow<T>(this T control, int row)
        where T : Control
    {
        Grid.SetRow(control, row);
        return control;
    }
}
