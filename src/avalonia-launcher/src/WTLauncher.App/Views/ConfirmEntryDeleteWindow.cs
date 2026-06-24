using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class ConfirmEntryDeleteWindow : Window
{
    public ConfirmEntryDeleteWindow(LauncherEntryDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Title = "删除启动按钮";
        Width = 520;
        Height = 260;
        MinWidth = 480;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        var cancel = LauncherDialogButtons.SafeDefaultCancel();
        cancel.Click += (_, _) => Close(false);

        var delete = LauncherDialogButtons.Destructive("删除启动按钮");
        delete.Click += (_, _) => Close(true);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    LauncherDialogText.Title("确认删除启动按钮？"),
                    LauncherDialogText.Body($"“{target.EntryName}” 会从 config\\config.json 删除，并从所有启动组里移除引用。"),
                    LauncherDialogButtons.Footer(24, cancel, delete),
                },
            },
        };
    }
}
