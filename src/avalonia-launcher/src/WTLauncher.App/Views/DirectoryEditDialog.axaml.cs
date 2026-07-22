using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace WTLauncher.App.Views;

public sealed partial class DirectoryEditDialog : Window
{
    public DirectoryEditDialog()
    {
        InitializeComponent();
    }

    public DirectoryEditDialog(string title, string label, string initialValue, int affectedEntryCount)
    {
        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        LabelText.Text = label;
        Input.Text = initialValue;

        if (affectedEntryCount > 0)
        {
            AffectedHint.Text = $"\u26a0 此地址被 {affectedEntryCount} 个 entry 引用 — 修改将自动同步所有引用 entry。";
            AffectedHint.IsVisible = true;
        }

        Opened += (_, _) => Input.Focus();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var startDir = !string.IsNullOrEmpty(Input.Text)
            ? await StorageProvider.TryGetFolderFromPathAsync(Input.Text)
            : null;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择目录",
            AllowMultiple = false,
            SuggestedStartLocation = startDir,
        });
        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } localPath)
            Input.Text = localPath;
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        var v = Input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(v))
        {
            ErrorText.Text = "地址不能为空。";
            ErrorText.IsVisible = true;
            return;
        }
        Close(v);
    }
}
