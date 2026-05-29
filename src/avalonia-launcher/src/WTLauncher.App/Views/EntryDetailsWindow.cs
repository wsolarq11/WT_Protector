using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class EntryDetailsWindow : Window
{
    private readonly TextBox _idBox;
    private readonly TextBox _nameBox;
    private readonly TextBox _directoryBox;
    private readonly TextBox _commandBox;
    private readonly CheckBox _selectedBox;
    private readonly TextBlock _errorText;

    public EntryDetailsWindow(LaunchEntry? entry)
    {
        var isEdit = entry is not null;
        Title = isEdit ? "编辑按钮" : "新增按钮";
        Width = 720;
        Height = 520;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        _idBox = CreateTextBox(entry?.Id ?? "");
        _nameBox = CreateTextBox(entry?.Name ?? "");
        _directoryBox = CreateTextBox(entry?.Directory ?? "");
        _commandBox = CreateTextBox(entry?.Command ?? "");
        _selectedBox = new CheckBox
        {
            Content = "保存为已勾选",
            IsChecked = entry?.Selected ?? false,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _errorText = LauncherDialogText.Error();

        var cancel = LauncherDialogButtons.Cancel();
        cancel.Click += (_, _) => Close((EntryDraft?)null);

        var save = LauncherDialogButtons.Primary(isEdit ? "保存按钮" : "新增按钮");
        save.Click += (_, _) => Submit();

        var footer = LauncherDialogButtons.Footer(16, cancel, save);

        var content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                LauncherDialogText.Title(isEdit ? "编辑启动按钮" : "新增启动按钮"),
                LauncherDialogText.Label("ID（稳定唯一，不要重复）"),
                _idBox,
                LauncherDialogText.Label("名称"),
                _nameBox,
                LauncherDialogText.Label("目录"),
                _directoryBox,
                LauncherDialogText.Label("命令"),
                _commandBox,
                LauncherDialogText.Subtle("启动确认由主窗口顶部开关统一控制；开启时所有启动项统一确认，关闭时所有启动项直接启动。"),
                _selectedBox,
                _errorText,
                footer,
            },
        };

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new ScrollViewer { Content = content },
        };

        Opened += (_, _) =>
        {
            (isEdit ? _nameBox : _idBox).Focus();
            (isEdit ? _nameBox : _idBox).SelectAll();
        };
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Text = text,
            FontSize = 14,
            Padding = new Thickness(10, 8),
        };
    }

    private void Submit()
    {
        var draft = LauncherEntryDrafts.NormalizeDraft(new EntryDraft(
            _idBox.Text ?? string.Empty,
            _nameBox.Text ?? string.Empty,
            _directoryBox.Text ?? string.Empty,
            _commandBox.Text ?? string.Empty,
            _selectedBox.IsChecked == true));

        if (!LauncherEntryDrafts.HasRequiredFields(draft))
        {
            _errorText.Text = "ID、名称、目录、命令都必须填写。";
            _errorText.IsVisible = true;
            return;
        }

        Close(draft);
    }
}
