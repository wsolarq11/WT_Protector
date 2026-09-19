using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 地址管理 dialog 的 view-model。与 CommandManagementViewModel 同模式: 单 tab,纯函数通过 callback 写回。
/// ponytail: 无 templates 概念,directory 字段直接去重 + 引用计数。
/// </summary>
public sealed partial class DirectoryManagementViewModel : ObservableObject
{
    private readonly IDirectoryEditApplier _applyEdit;

    public ObservableCollection<DirectoryRow> DirectoryRows { get; } = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDirectory))]
    private DirectoryRow? _selectedDirectoryRow;

    public bool HasSelectedDirectory => SelectedDirectoryRow is not null;
    public bool HasNoDirectoryRows => DirectoryRows.Count == 0;

#pragma warning disable CA1822
    public string WindowTitle => LauncherLocalization.Format("dirmgmt.title");
    public string Heading => LauncherLocalization.Format("dirmgmt.heading");
    public string Description => LauncherLocalization.Format("dirmgmt.description");
    public string EditRenameLabel => LauncherLocalization.Format("dirmgmt.editRename");
    public string EditRenameAutomation => LauncherLocalization.Format("dirmgmt.editRenameAutomation");
    public string MergeLabel => LauncherLocalization.Format("dirmgmt.merge");
    public string MergeAutomation => LauncherLocalization.Format("dirmgmt.mergeAutomation");
    public string DeleteLabel => LauncherLocalization.Format("dirmgmt.delete");
    public string DeleteAutomation => LauncherLocalization.Format("dirmgmt.deleteAutomation");
    public string ReferencePrefix => LauncherLocalization.Format("dirmgmt.referencePrefix");
    public string EmptyLabel => LauncherLocalization.Format("dirmgmt.empty");
    public string EmptyAutomation => LauncherLocalization.Format("dirmgmt.emptyAutomation");
    public string CloseLabel => LauncherLocalization.Format("dirmgmt.close");
    public string CloseAutomation => LauncherLocalization.Format("dirmgmt.closeAutomation");
#pragma warning restore CA1822

    public DirectoryManagementViewModel(
        IReadOnlyList<DirectoryRow> initialRows,
        IDirectoryEditApplier applyEdit)
    {
        ArgumentNullException.ThrowIfNull(initialRows);
        _applyEdit = applyEdit ?? throw new ArgumentNullException(nameof(applyEdit));
        foreach (var r in initialRows) DirectoryRows.Add(r);
    }

    private void Apply(DirectoryManagementEdit edit)
    {
        DirectoryManagementResult result;
        try
        {
            result = _applyEdit.Apply(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Apply");
            StatusMessage = LauncherLocalization.Format("maintenance.applyFailed", ExceptionMessageFormatter.ForUser(ex));
            return;
        }

        StatusMessage = result.StatusMessage;
        if (!result.Success)
            return;

        DirectoryRows.Clear();
        foreach (var r in result.UpdatedDirectoryRows) DirectoryRows.Add(r);
    }

    [RelayCommand]
    private void RenameDirectory(string newDirectory)
    {
        if (SelectedDirectoryRow is null || string.IsNullOrWhiteSpace(newDirectory))
            return;
        Apply(new DirectoryManagementEdit.RenameDirectory(SelectedDirectoryRow.Directory, newDirectory.Trim()));
    }

    [RelayCommand]
    private void MergeDirectories(string keeperDirectory)
    {
        if (SelectedDirectoryRow is null || string.IsNullOrWhiteSpace(keeperDirectory))
            return;
        Apply(new DirectoryManagementEdit.MergeDirectories(
            LoserDirectory: SelectedDirectoryRow.Directory,
            KeeperDirectory: keeperDirectory.Trim()));
    }

    [RelayCommand]
    private void DeleteDirectory()
    {
        if (SelectedDirectoryRow is null)
            return;
        if (SelectedDirectoryRow.ReferenceCount > 0)
        {
            StatusMessage = LauncherLocalization.Format("dirmgmt.cannotDeleteDirectory", SelectedDirectoryRow.ReferenceCount);
            return;
        }
        Apply(new DirectoryManagementEdit.DeleteDirectory(SelectedDirectoryRow.Directory));
    }
}
