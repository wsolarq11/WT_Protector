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
            Text = plan.CommandLine,
            IsReadOnly = true,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 12,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
        };
        var cancel = LauncherDialogButtons.Cancel();
        cancel.Click += (_, _) => Close(false);

        var launch = LauncherDialogButtons.Primary("确认启动");
        launch.Click += (_, _) => Close(true);

        Content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 14,
            Children =
            {
                title,
                warning.WithGridRow(1),
                command.WithGridRow(2),
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, launch },
                }.WithGridRow(3),
            },
        };
    }
}
