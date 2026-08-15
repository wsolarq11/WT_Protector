using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class EntryDetailsWindow : Window
{
    private readonly bool _isEdit;
    private readonly IReadOnlyDictionary<string, string>? _commandTemplates;
    private readonly IEntryDraftValidator _validator;

    /// <summary>Pre-fill data for the entry editor dialog. Lives in the Views namespace
    /// to avoid leaking Core types into the View layer.</summary>
    public sealed record EntryPrefill(
        string? Id,
        string? Name,
        string? Directory,
        string? Command,
        bool Selected);

    public EntryDetailsWindow()
    {
        _commandTemplates = null;
        _validator = new EntryDraftValidator();
        InitializeComponent();
    }

    public EntryDetailsWindow(
        EntryPrefill? prefill,
        int presetReferenceCount = 0,
        IReadOnlyDictionary<string, string>? commandTemplates = null,
        IEntryDraftValidator? validator = null)
    {
        _isEdit = prefill is not null;
        _commandTemplates = commandTemplates;
        _validator = validator ?? new EntryDraftValidator();

        InitializeComponent();

        Title = _isEdit ? "编辑按钮" : "新增按钮";
        TitleText.Text = _isEdit ? "编辑启动按钮" : "新增启动按钮";

        IdBox.Text = RequireInitialEntryId(prefill);
        IdBox.MaxLength = 256;
        NameBox.Text = RequireInitialEntryText(prefill, item => item.Name, "name");
        NameBox.MaxLength = 4096;
        DirectoryBox.Text = RequireInitialEntryText(prefill, item => item.Directory, "directory");
        DirectoryBox.MaxLength = 4096;
        CommandBox.Text = RequireInitialEntryText(prefill, item => item.Command, "command");
        CommandBox.MaxLength = 4096;

        // R5 (friend-handoff): in add mode, lower the barrier — ID may stay blank
        // (auto-derived from the name on save), the command defaults to pwsh, and
        // the directory is browsable. Edit mode keeps the existing behavior.
        if (!_isEdit)
        {
            IdBox.PlaceholderText = "留空自动生成";
            CommandBox.Text = "pwsh";
        }

        SelectedBox.IsChecked = prefill?.Selected ?? false;

        if (_isEdit)
        {
            var hint = presetReferenceCount == 0
                ? "此 ID 未被任何启动组引用"
                : $"此 ID 被 {presetReferenceCount} 个启动组引用";
            PresetHintText.Text = hint;
            PresetHintText.IsVisible = true;
        }

        SaveButton.Content = _isEdit ? "保存按钮" : "新增按钮";

        SetupTemplatePreview();

        Opened += (_, _) =>
        {
            // Defer SelectAll to Loaded priority so the TextBox has fully
            // acquired keyboard focus before the selection is applied. Calling
            // SelectAll immediately after Focus() can lose the selection because
            // the focus handler hasn't completed yet.
            var target = _isEdit ? NameBox : IdBox;
            target.Focus();
            Dispatcher.UIThread.Post(() => target.SelectAll(), DispatcherPriority.Loaded);
        };
    }

    private void SetupTemplatePreview()
    {
        if (_commandTemplates is null || _commandTemplates.Count == 0)
            return;

        var templateKeys = string.Join("、", _commandTemplates.Select(kv => $"${kv.Key}"));
        TemplateSubtleText.Text = $"可用模板：{templateKeys}";
        TemplateSubtleText.IsVisible = true;

        CommandBox.TextChanged += (_, _) =>
        {
            var cmd = _validator.NormalizeText(CommandBox.Text);
            if (cmd.StartsWith('$') && cmd.Length > 1)
            {
                var key = cmd[1..];
                if (_commandTemplates.TryGetValue(key, out var resolved))
                    TemplatePreview.Text = $"\u2192 {resolved}";
                else
                    TemplatePreview.Text = $"\u26a0 \u672a\u5b9a\u4e49\u7684\u6a21\u677f: ${key}";
                TemplatePreview.IsVisible = true;
            }
            else
            {
                TemplatePreview.IsVisible = false;
            }
        };

        // Trigger initial preview for edit mode
        var initialCmd = _validator.NormalizeText(CommandBox.Text ?? "");
        if (initialCmd.StartsWith('$') && initialCmd.Length > 1)
        {
            var key = initialCmd[1..];
            if (_commandTemplates.TryGetValue(key, out var resolved))
            {
                TemplatePreview.Text = $"\u2192 {resolved}";
                TemplatePreview.IsVisible = true;
            }
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close((EntryDraft?)null);

    private void OnSaveClicked(object? sender, RoutedEventArgs e) => Submit();

    private async void OnBrowseDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var startDir = !string.IsNullOrEmpty(DirectoryBox.Text)
                ? await StorageProvider.TryGetFolderFromPathAsync(DirectoryBox.Text)
                : null;
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择目录",
                AllowMultiple = false,
                SuggestedStartLocation = startDir,
            });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } localPath)
                DirectoryBox.Text = localPath;
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "EntryDetails.BrowseDirectory");
        }
    }

    private void Submit()
    {
        try
        {
            var rawDraft = new EntryDraft(
                ResolveSubmittedId(),
                RequireSubmittedText(NameBox, "name"),
                RequireSubmittedText(DirectoryBox, "directory"),
                RequireSubmittedText(CommandBox, "command"),
                SelectedBox.IsChecked == true);

            if (!_validator.TryNormalizeRequiredDraft(rawDraft, out var draft, out var requiredFieldError))
            {
                ErrorText.Text = requiredFieldError;
                ErrorText.IsVisible = true;
                return;
            }

            Close(draft);
        }
        catch (InvalidDataException ex)
        {
            ErrorText.Text = ExceptionMessageFormatter.ForUser(ex);
            ErrorText.IsVisible = true;
        }
    }

    /// <summary>
    /// Add-mode ID derivation: <see cref="EntryId.Normalize"/> of the name,
    /// with a timestamp fallback for names that cannot form an ID
    /// (e.g. non-Latin). Uniqueness stays enforced by the service layer.
    /// </summary>
    public static string ResolveAutoId(string? name, DateTime now) =>
        new EntryDraftValidator().ResolveAutoId(name, now);

    /// <summary>
    /// Add-mode ID resolution: blank ID is auto-derived from the name.
    /// Edit mode always uses the submitted value.
    /// </summary>
    private string ResolveSubmittedId()
    {
        var submitted = RequireSubmittedText(IdBox, "id");
        if (_isEdit || submitted.Length > 0)
            return submitted;

        return ResolveAutoId(RequireSubmittedText(NameBox, "name"), DateTime.Now);
    }

    private string RequireInitialEntryId(EntryPrefill? prefill)
    {
        if (prefill is null)
            return string.Empty;

        var id = _validator.NormalizeId(prefill.Id);
        if (!_validator.IsUsableId(id))
            throw new InvalidDataException("Launcher entry dialog initial id is required.");

        return id;
    }

    private string RequireInitialEntryText(
        EntryPrefill? prefill,
        Func<EntryPrefill, string?> selectText,
        string fieldName)
    {
        if (prefill is null)
            return string.Empty;

        var text = _validator.NormalizeText(selectText(prefill));
        if (text.Length == 0)
            throw new InvalidDataException($"Launcher entry dialog initial {fieldName} is required.");

        return text;
    }

    /// <summary>
    /// Avalonia <see cref="TextBox.Text"/> never returns null — it coalesces to "".
    /// The null-coalescing guard was dead code and has been removed.
    /// </summary>
    private static string RequireSubmittedText(TextBox textBox, string fieldName) =>
        textBox.Text ?? "";
}
