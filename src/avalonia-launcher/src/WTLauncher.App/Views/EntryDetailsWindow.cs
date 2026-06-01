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

        _idBox = LauncherDialogText.Input(RequireInitialEntryId(entry));
        _nameBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Name, "name"));
        _directoryBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Directory, "directory"));
        _commandBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Command, "command"));
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

    private void Submit()
    {
        var rawDraft = new EntryDraft(
            RequireSubmittedText(_idBox, "id"),
            RequireSubmittedText(_nameBox, "name"),
            RequireSubmittedText(_directoryBox, "directory"),
            RequireSubmittedText(_commandBox, "command"),
            _selectedBox.IsChecked == true);

        if (!LauncherEntryDrafts.TryNormalizeRequiredDraft(rawDraft, out var draft, out var requiredFieldError))
        {
            _errorText.Text = requiredFieldError;
            _errorText.IsVisible = true;
            return;
        }

        Close(draft);
    }

    private static string RequireInitialEntryId(LaunchEntry? entry)
    {
        if (entry is null)
            return string.Empty;

        var id = LauncherEntryIds.Normalize(entry.Id);
        if (!LauncherEntryIds.IsUsable(id))
            throw new InvalidDataException("Launcher entry dialog initial id is required.");

        return id;
    }

    private static string RequireInitialEntryText(
        LaunchEntry? entry,
        Func<LaunchEntry, string?> selectText,
        string fieldName)
    {
        if (entry is null)
            return string.Empty;

        var text = LauncherEntryText.Normalize(selectText(entry));
        if (text.Length == 0)
            throw new InvalidDataException($"Launcher entry dialog initial {fieldName} is required.");

        return text;
    }

    private static string RequireSubmittedText(TextBox textBox, string fieldName) =>
        textBox.Text
        ?? throw new InvalidDataException($"Launcher entry dialog submitted {fieldName} text is null.");
}
