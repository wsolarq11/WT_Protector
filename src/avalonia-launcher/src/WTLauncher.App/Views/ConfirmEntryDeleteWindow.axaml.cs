using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmEntryDeleteWindow : Window
{
    public ConfirmEntryDeleteWindow()
    {
        InitializeComponent();
    }

    public ConfirmEntryDeleteWindow(LauncherEntryDeleteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        InitializeComponent();
        BodyText.Text = $"\u201c{target.EntryName}\u201d 会从 config\\launcher.json 删除，并从所有启动组里移除引用。";
        Opened += (_, _) => CancelButton.Focus();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnDeleteClicked(object? sender, RoutedEventArgs e) => Close(true);
}
