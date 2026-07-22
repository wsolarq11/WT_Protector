using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmPresetDeleteWindow : Window
{
    public ConfirmPresetDeleteWindow()
    {
        InitializeComponent();
    }

    public ConfirmPresetDeleteWindow(LauncherPresetDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        InitializeComponent();
        BodyText.Text = $"\u201c{target.PresetName}\u201d 只会删除命名组，不会删除任何启动项。删除后可重新保存。";
        SubtleText.Text = $"包含 {target.EntryCount} 个启动项。";
        Opened += (_, _) => CancelButton.Focus();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnDeleteClicked(object? sender, RoutedEventArgs e) => Close(true);
}
