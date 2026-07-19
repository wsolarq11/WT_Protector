using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// 地址编辑二级 dialog。单字段 directory + "📁 浏览..." 按钮调 OpenFolderDialog。
/// 显示"将影响 N 个 entry"提示,让用户在 cascade 重命名前明确感知。
/// ponytail: 单文件,基于 LauncherDialogText/Button helpers,无 XAML。
/// </summary>
public sealed class DirectoryEditDialog : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";
    private readonly TextBox _input;
    private readonly TextBlock _error;
    private readonly Button _browseButton;

    public DirectoryEditDialog(string title, string label, string initialValue, int affectedEntryCount)
    {
        Title = title;
        Width = 600;
        MinWidth = 400;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        var titleLabel = LauncherDialogText.Title(title);
        titleLabel.Margin = new Thickness(0, 0, 0, 12);

        var labelBlock = LauncherDialogText.Label(label);
        labelBlock.Margin = new Thickness(0, 0, 0, 4);
        _input = LauncherDialogText.Input(initialValue);
        _input.FontFamily = new FontFamily(MonospaceFontFamily);
        _input.Watermark = @"D:\path\to\directory";

        _browseButton = new Button
        {
            Content = "📁 浏览...",
            Classes = { "secondary" },
            MinWidth = 100,
        };
        _browseButton.Click += async (_, _) =>
        {
            // ponytail: Avalonia 12 StorageProvider API — replaces OpenFolderDialog.
            var startDir = !string.IsNullOrEmpty(_input.Text)
                ? await StorageProvider.TryGetFolderFromPathAsync(_input.Text)
                : null;
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择目录",
                AllowMultiple = false,
                SuggestedStartLocation = startDir,
            });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } localPath)
                _input.Text = localPath;
        };

        var inputRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,Auto"),
            Children =
            {
                _input.WithGridColumn(0),
                _browseButton.WithGridColumn(2),
            },
        };

        TextBlock? affectedHint = null;
        if (affectedEntryCount > 0)
        {
            affectedHint = new TextBlock
            {
                Text = $"⚠ 此地址被 {affectedEntryCount} 个 entry 引用 — 修改将自动同步所有引用 entry。",
                FontSize = 11,
                Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
            };
        }

        _error = LauncherDialogText.Error();
        _error.Margin = new Thickness(0, 8, 0, 0);

        var ok = LauncherDialogButtons.Primary("确定", minWidth: 96);
        ok.Click += (_, _) => OnOk();
        var cancel = LauncherDialogButtons.Cancel(minWidth: 96);
        cancel.Click += (_, _) => Close(null);
        var footer = LauncherDialogButtons.Footer(LauncherDesignTokens.DialogFooterTopMargin, cancel, ok);

        var fieldsStack = new StackPanel { Spacing = 4 };
        fieldsStack.Children.Add(labelBlock);
        fieldsStack.Children.Add(inputRow);
        if (affectedHint is not null) fieldsStack.Children.Add(affectedHint);
        fieldsStack.Children.Add(_error);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            Children =
            {
                titleLabel.WithGridRow(0),
                fieldsStack.WithGridRow(1),
                footer.WithGridRow(2),
            },
        };

        Content = new Border
        {
            Padding = new Thickness(LauncherDesignTokens.DialogOuterPadding),
            Child = grid,
        };

        Opened += (_, _) => _input.Focus();
    }

    private void OnOk()
    {
        var v = _input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(v))
        {
            _error.Text = "地址不能为空。";
            _error.IsVisible = true;
            return;
        }
        Close(v);
    }
}
