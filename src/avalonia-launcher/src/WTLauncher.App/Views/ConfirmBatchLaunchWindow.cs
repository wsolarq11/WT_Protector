using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
        var title = LauncherDialogText.Title("一键启动已勾选项？");
        var messageText = LauncherDialogText.Body(message);
        messageText.Margin = new Thickness(0, 12, 0, 14);
        var namesText = LauncherDialogText.Subtle(names);

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
                            Content = namesText,
                        },
                    }.WithGridRow(2),
                    LauncherDialogButtons.Footer(18, cancel, launch).WithGridRow(3),
                },
            },
        };
    }
}
