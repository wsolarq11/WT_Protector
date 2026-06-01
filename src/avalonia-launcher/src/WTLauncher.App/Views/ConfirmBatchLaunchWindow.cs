using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed class ConfirmBatchLaunchWindow : Window
{
    public ConfirmBatchLaunchWindow(LauncherBatchLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        Title = "确认一键启动";
        Width = 760;
        Height = 520;
        MinWidth = 460;
        MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var items = confirmation.Items;
        var plans = LauncherBatchLaunchPlanText.Format(items);
        var message = $"将启动 {items.Count} 个{confirmation.TargetLabel}。请确认实际 wt.exe 命令。";
        var title = LauncherDialogText.Title($"{confirmation.TargetLabel}启动确认？");
        var messageText = LauncherDialogText.Body(message);
        messageText.Margin = new Thickness(0, 12, 0, 14);
        var plansText = new TextBox
        {
            Text = plans,
            IsReadOnly = true,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 12,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 220,
        };

        var cancel = LauncherDialogButtons.Cancel(minWidth: 96);
        cancel.Click += (_, _) => Close(false);

        var launch = LauncherDialogButtons.Primary("确认启动", minWidth: 116);
        launch.Click += (_, _) => Close(true);

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
                Children =
                {
                    title,
                    messageText.WithGridRow(1),
                    new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        BorderBrush = Brushes.LightGray,
                        Padding = new Thickness(12),
                        Child = new ScrollViewer
                        {
                            Content = plansText,
                        },
                    }.WithGridRow(2),
                    LauncherDialogButtons.Footer(18, cancel, launch).WithGridRow(3),
                },
            },
        };
    }
}
