using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed class ConfirmPresetDeleteWindow : Window
{
    public ConfirmPresetDeleteWindow(LauncherPresetViewModel preset)
    {
        Title = "删除启动组";
        Width = 560;
        Height = 280;
        MinWidth = 460;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var cancel = LauncherDialogButtons.SafeDefaultCancel();
        cancel.Click += (_, _) => Close(false);

        var delete = LauncherDialogButtons.Destructive("删除启动组");
        delete.Click += (_, _) => Close(true);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                RowSpacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "删除这个启动组？",
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#0F172A"),
                    },
                    new TextBlock
                    {
                        Text = $"\"{preset.Name}\" 只会删除命名组，不会删除任何启动项。删除后可重新保存。",
                        Foreground = Brush.Parse("#475569"),
                        TextWrapping = TextWrapping.Wrap,
                    }.WithGridRow(1),
                    new TextBlock
                    {
                        Text = $"包含 {preset.EntryIds.Count} 个启动项。",
                        Foreground = Brush.Parse("#64748B"),
                    }.WithGridRow(2),
                    LauncherDialogButtons.Footer(14, cancel, delete).WithGridRow(3),
                },
            },
        };
    }
}
