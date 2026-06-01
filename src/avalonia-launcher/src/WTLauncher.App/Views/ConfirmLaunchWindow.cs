using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed class ConfirmLaunchWindow : Window
{
    public ConfirmLaunchWindow(LauncherSingleLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        Title = "确认启动";
        Width = 680;
        Height = 360;
        MinWidth = 560;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var title = LauncherDialogText.Title($"启动：{confirmation.EntryName}");
        var warning = LauncherDialogText.Body("当前已开启启动确认，请确认后继续。");
        var command = new TextBox
        {
            Text = confirmation.Plan.CommandLine,
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
                LauncherDialogButtons.Footer(0, cancel, launch).WithGridRow(3),
            },
        };
    }
}
