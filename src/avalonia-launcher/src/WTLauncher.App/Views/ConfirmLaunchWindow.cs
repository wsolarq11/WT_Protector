using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class ConfirmLaunchWindow : Window
{
    public ConfirmLaunchWindow(LaunchEntry entry, LaunchPlan plan)
    {
        Title = "确认启动";
        Width = 680;
        Height = 360;
        MinWidth = 560;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        Content = BuildContent(entry, plan);
    }

    private Control BuildContent(LaunchEntry entry, LaunchPlan plan)
    {
        var title = new TextBlock
        {
            Text = $"启动：{entry.Name}",
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#0F172A"),
        };
        var warning = new TextBlock
        {
            Text = "当前已开启启动确认，请确认后继续。",
            Foreground = Brush.Parse("#475569"),
            TextWrapping = TextWrapping.Wrap,
        };
        var command = new TextBox
        {
            Text = $"{plan.Executable} {plan.Arguments}",
            IsReadOnly = true,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 12,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
        };
        var cancel = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 8),
            IsCancel = true,
        };
        cancel.Click += (_, _) => Close(false);

        var launch = new Button
        {
            Content = "确认启动",
            Padding = new Thickness(18, 8),
            Background = Brush.Parse("#2563EB"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            IsDefault = true,
        };
        launch.Click += (_, _) => Close(true);

        return new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 14,
            Children =
            {
                title,
                Place(warning, 1),
                Place(command, 2),
                Place(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, launch },
                }, 3),
            },
        };
    }

    private static T Place<T>(T control, int row)
        where T : Control
    {
        Grid.SetRow(control, row);
        return control;
    }
}
