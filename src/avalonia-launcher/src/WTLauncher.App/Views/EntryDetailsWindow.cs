using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System.Linq;
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

    public EntryDetailsWindow(LaunchEntry? entry, int presetReferenceCount = 0, IReadOnlyDictionary<string, string>? commandTemplates = null)
    {
        var isEdit = entry is not null;
        Title = isEdit ? "编辑按钮" : "新增按钮";
        Width = 720;
        Height = 560;
        MinWidth = 560;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        _idBox = LauncherDialogText.Input(RequireInitialEntryId(entry));
        _nameBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Name, "name"));
        _directoryBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Directory, "directory"));
        _commandBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Command, "command"));
        // MaxLength must match store-level limits to prevent overwrites that the
        // store will later reject. LauncherEntryIds.Normalize truncates at 256;
        // LauncherConfigStore.RequireEntryText rejects >4096.
        _idBox.MaxLength = 256;
        _nameBox.MaxLength = 4096;
        _directoryBox.MaxLength = 4096;
        _commandBox.MaxLength = 4096;
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

        List<Control> children = [];
        children.Add(LauncherDialogText.Title(isEdit ? "编辑启动按钮" : "新增启动按钮"));
        children.Add(LauncherDialogText.Label("ID（稳定唯一，不要重复）"));
        children.Add(_idBox);

        if (isEdit)
        {
            var hint = presetReferenceCount == 0
                ? "此 ID 未被任何启动组引用"
                : $"此 ID 被 {presetReferenceCount} 个启动组引用";
            children.Add(LauncherDialogText.Subtle(hint));
        }

        children.Add(LauncherDialogText.Label("名称"));
        children.Add(_nameBox);
        children.Add(LauncherDialogText.Label("目录"));
        children.Add(_directoryBox);
        children.Add(LauncherDialogText.Label("命令"));
        children.Add(_commandBox);
        if (commandTemplates is not null && commandTemplates.Count > 0)
        {
            var templateKeys = string.Join("、", commandTemplates.Select(kv => $"${kv.Key}"));
            children.Add(LauncherDialogText.Subtle($"可用模板：{templateKeys}"));
            var templatePreview = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.Parse("#0369A1")),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
                IsVisible = false,
            };
            _commandBox.TextChanged += (_, _) =>
            {
                var cmd = LauncherEntryText.Normalize(_commandBox.Text);
                if (cmd.StartsWith("$") && cmd.Length > 1)
                {
                    var key = cmd[1..];
                    if (commandTemplates.TryGetValue(key, out var resolved))
                        templatePreview.Text = $"→ {resolved}";
                    else
                        templatePreview.Text = $"⚠ 未定义的模板: ${key}";
                    templatePreview.IsVisible = true;
                }
                else
                {
                    templatePreview.IsVisible = false;
                }
            };
            // Trigger initial preview for edit mode
            var initialCmd = LauncherEntryText.Normalize(_commandBox.Text ?? "");
            if (initialCmd.StartsWith("$") && initialCmd.Length > 1)
            {
                var key = initialCmd[1..];
                if (commandTemplates.TryGetValue(key, out var resolved))
                {
                    templatePreview.Text = $"→ {resolved}";
                    templatePreview.IsVisible = true;
                }
            }
            children.Add(templatePreview);
        }
        children.Add(LauncherDialogText.Subtle("启动确认由主窗口顶部开关统一控制；开启时所有启动项统一确认，关闭时所有启动项直接启动。"));
        children.Add(_selectedBox);
        children.Add(_errorText);
        children.Add(footer);

        var content = new StackPanel { Spacing = 10 };
        foreach (var child in children)
            content.Children.Add(child);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new ScrollViewer { Content = content },
        };

        Opened += (_, _) =>
        {
            // Defer SelectAll to Loaded priority so the TextBox has fully
            // acquired keyboard focus before the selection is applied. Calling
            // SelectAll immediately after Focus() can lose the selection because
            // the focus handler hasn't completed yet.
            var target = isEdit ? _nameBox : _idBox;
            target.Focus();
            Dispatcher.UIThread.Post(() => target.SelectAll(), DispatcherPriority.Loaded);
        };
    }

    private void Submit()
    {
        try
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
        catch (InvalidDataException ex)
        {
            _errorText.Text = ex.Message;
            _errorText.IsVisible = true;
        }
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

    /// <summary>
    /// Avalonia <see cref="TextBox.Text"/> never returns null — it coalesces to "".
    /// The null-coalescing guard was dead code and has been removed.
    /// </summary>
    private static string RequireSubmittedText(TextBox textBox, string fieldName) =>
        textBox.Text!; // Avalonia TextBox.Text never returns null — coalesces to ""
}
