using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class ConfirmPresetDeleteWindow : Window
{
    public ConfirmPresetDeleteWindow(LauncherPresetDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Title = "删除启动组";
        Width = 560;
        Height = 280;
        MinWidth = 460;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        var cancel = LauncherDialogButtons.Cancel(isDefault: true);
        cancel.Click += (_, _) => Close(false);

        var delete = LauncherDialogButtons.Destructive("删除启动组", isDefault: false);
        delete.Click += (_, _) => Close(true);

        Opened += (_, _) => delete.Focus();

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                RowSpacing = 12,
                Children =
                {
                    LauncherDialogText.Title("删除这个启动组？"),
                    LauncherDialogText.Body($"\"{target.PresetName}\" 只会删除命名组，不会删除任何启动项。删除后可重新保存。").WithGridRow(1),
                    LauncherDialogText.Subtle($"包含 {target.EntryCount} 个启动项。").WithGridRow(2),
                    LauncherDialogButtons.Footer(14, cancel, delete).WithGridRow(3),
                },
            },
        };
    }
}
