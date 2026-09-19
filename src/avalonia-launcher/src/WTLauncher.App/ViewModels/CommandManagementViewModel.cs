using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 命令管理 dialog 的 view-model。Tab 1 = 命令引用 (entries 去重),Tab 2 = 命令模板 (commandTemplates)。
/// ponytail: 单 VM,两个 ObservableCollection 共享 snapshot/rollback 路径(由 owner = MainWindowViewModel 通过 editCallback 提供)。
/// 与 DiscoverAgentsViewModel 同模式: VM 不直接 mutate store,只通过 Func 把 edit 交回 owner。
/// </summary>
public sealed partial class CommandManagementViewModel : ObservableObject
{
    private readonly ICommandEditApplier _applyEdit;

    public ObservableCollection<CommandRow> CommandRows { get; } = new();
    public ObservableCollection<TemplateRow> TemplateRows { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCommandTabActive))]
    [NotifyPropertyChangedFor(nameof(IsTemplateTabActive))]
    private int _activeTab; // 0 = 命令引用, 1 = 命令模板

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCommand))]
    private CommandRow? _selectedCommandRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTemplate))]
    private TemplateRow? _selectedTemplateRow;

    public bool IsCommandTabActive => ActiveTab == 0;
    public bool IsTemplateTabActive => ActiveTab == 1;
    public bool HasCommandRows => CommandRows.Count > 0;
    public bool HasNoCommandRows => !HasCommandRows;
    public bool HasTemplateRows => TemplateRows.Count > 0;
    public bool HasNoTemplateRows => !HasTemplateRows;
    public bool HasSelectedCommand => SelectedCommandRow is not null;
    public bool HasSelectedTemplate => SelectedTemplateRow is not null;

#pragma warning disable CA1822
    public string WindowTitle => LauncherLocalization.Format("cmdmgmt.title");
    public string Heading => LauncherLocalization.Format("cmdmgmt.heading");
    public string Description => LauncherLocalization.Format("cmdmgmt.description");
    public string CommandTab => LauncherLocalization.Format("cmdmgmt.commandTab");
    public string CommandTabAutomation => LauncherLocalization.Format("cmdmgmt.commandTabAutomation");
    public string TemplateTab => LauncherLocalization.Format("cmdmgmt.templateTab");
    public string TemplateTabAutomation => LauncherLocalization.Format("cmdmgmt.templateTabAutomation");
    public string EditRenameLabel => LauncherLocalization.Format("cmdmgmt.editRename");
    public string EditRenameAutomation => LauncherLocalization.Format("cmdmgmt.editRenameAutomation");
    public string DeleteLabel => LauncherLocalization.Format("cmdmgmt.delete");
    public string DeleteAutomation => LauncherLocalization.Format("cmdmgmt.deleteAutomation");
    public string NewTemplateLabel => LauncherLocalization.Format("cmdmgmt.newTemplate");
    public string NewTemplateAutomation => LauncherLocalization.Format("cmdmgmt.newTemplateAutomation");
    public string UpdateKeyLabel => LauncherLocalization.Format("cmdmgmt.updateKey");
    public string UpdateKeyAutomation => LauncherLocalization.Format("cmdmgmt.updateKeyAutomation");
    public string UpdateValueLabel => LauncherLocalization.Format("cmdmgmt.updateValue");
    public string UpdateValueAutomation => LauncherLocalization.Format("cmdmgmt.updateValueAutomation");
    public string DeleteTemplateLabel => LauncherLocalization.Format("cmdmgmt.deleteTemplate");
    public string DeleteTemplateAutomation => LauncherLocalization.Format("cmdmgmt.deleteTemplateAutomation");
    public string ReferencePrefix => LauncherLocalization.Format("cmdmgmt.referencePrefix");
    public string EmptyCommandsLabel => LauncherLocalization.Format("cmdmgmt.emptyCommands");
    public string EmptyCommandsAutomation => LauncherLocalization.Format("cmdmgmt.emptyCommandsAutomation");
    public string EmptyTemplatesLabel => LauncherLocalization.Format("cmdmgmt.emptyTemplates");
    public string EmptyTemplatesAutomation => LauncherLocalization.Format("cmdmgmt.emptyTemplatesAutomation");
    public string CloseLabel => LauncherLocalization.Format("cmdmgmt.close");
    public string CloseAutomation => LauncherLocalization.Format("cmdmgmt.closeAutomation");
#pragma warning restore CA1822

    public CommandManagementViewModel(
        IReadOnlyList<CommandRow> initialCommandRows,
        IReadOnlyList<TemplateRow> initialTemplateRows,
        ICommandEditApplier applyEdit)
    {
        ArgumentNullException.ThrowIfNull(initialCommandRows);
        ArgumentNullException.ThrowIfNull(initialTemplateRows);
        _applyEdit = applyEdit ?? throw new ArgumentNullException(nameof(applyEdit));

        foreach (var r in initialCommandRows) CommandRows.Add(r);
        foreach (var r in initialTemplateRows) TemplateRows.Add(r);
    }

    /// <summary>所有 IReadOnlyList 模板 key,给 EditDialog 下拉用。</summary>
    public IReadOnlyList<string> TemplateKeys => TemplateRows.Select(r => r.Key).ToList();

    partial void OnActiveTabChanged(int value) => StatusMessage = string.Empty;

    [RelayCommand]
    private void SelectCommandTab() => ActiveTab = 0;

    [RelayCommand]
    private void SelectTemplateTab() => ActiveTab = 1;

    /// <summary>
    /// 应用一个 edit。失败 (Success=false) 时只刷状态消息,不动 rows;
    /// 成功时用 result 里的最新 rows 替换。
    /// </summary>
    private void Apply(CommandManagementEdit edit)
    {
        CommandManagementResult result;
        try
        {
            result = _applyEdit.Apply(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.Apply");
            StatusMessage = LauncherLocalization.Format("maintenance.applyFailed", ExceptionMessageFormatter.ForUser(ex));
            return;
        }

        StatusMessage = result.StatusMessage;
        if (!result.Success)
            return;

        CommandRows.Clear();
        foreach (var r in result.UpdatedCommandRows) CommandRows.Add(r);
        TemplateRows.Clear();
        foreach (var r in result.UpdatedTemplateRows) TemplateRows.Add(r);
        OnPropertyChanged(nameof(TemplateKeys));
        OnPropertyChanged(nameof(HasCommandRows));
        OnPropertyChanged(nameof(HasNoCommandRows));
        OnPropertyChanged(nameof(HasTemplateRows));
        OnPropertyChanged(nameof(HasNoTemplateRows));
    }

    // — Command 引用 tab 的操作 ———————————————————————

    [RelayCommand]
    private void RenameCommand(string newCommand)
    {
        if (SelectedCommandRow is null || string.IsNullOrWhiteSpace(newCommand))
            return;
        Apply(new CommandManagementEdit.RenameCommand(SelectedCommandRow.Command, newCommand.Trim()));
    }

    [RelayCommand]
    private void DeleteCommand()
    {
        if (SelectedCommandRow is null)
            return;
        if (SelectedCommandRow.ReferenceCount > 0)
        {
            StatusMessage = LauncherLocalization.Format("cmdmgmt.cannotDeleteCommand", SelectedCommandRow.ReferenceCount);
            return;
        }
        Apply(new CommandManagementEdit.DeleteCommand(SelectedCommandRow.Command));
    }

    // — Command 模板 tab 的操作 ———————————————————————

    [RelayCommand]
    private void UpdateTemplateKey(string newKey)
    {
        if (SelectedTemplateRow is null || string.IsNullOrWhiteSpace(newKey))
            return;
        Apply(new CommandManagementEdit.UpdateTemplateKey(
            OldKey: SelectedTemplateRow.Key,
            NewKey: newKey.Trim(),
            ExpandedValue: SelectedTemplateRow.ExpandedValue));
    }

    [RelayCommand]
    private void UpdateTemplateValue(string newValue)
    {
        if (SelectedTemplateRow is null)
            return;
        Apply(new CommandManagementEdit.UpdateTemplateValue(SelectedTemplateRow.Key, newValue));
    }

    [RelayCommand]
    private void AddTemplate(string keyAndValue)
    {
        // ponytail: dialog passes "key\tvalue" via single string to dodge [RelayCommand] tuple limits.
        var parts = keyAndValue.Split('\t');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            return;
        Apply(new CommandManagementEdit.AddTemplate(parts[0].Trim(), parts[1]));
    }

    [RelayCommand]
    private void DeleteTemplate()
    {
        if (SelectedTemplateRow is null)
            return;
        if (SelectedTemplateRow.ReferenceCount > 0)
        {
            StatusMessage = LauncherLocalization.Format("cmdmgmt.cannotDeleteTemplate", SelectedTemplateRow.ReferenceCount);
            return;
        }
        Apply(new CommandManagementEdit.DeleteTemplate(SelectedTemplateRow.Key));
    }
}
