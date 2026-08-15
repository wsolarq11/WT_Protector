using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
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
            StatusMessage = $"应用失败: {ExceptionMessageFormatter.ForUser(ex)}";
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
            StatusMessage = $"无法删除：{SelectedDirectoryRow.ReferenceCount} 个 entry 仍在引用此地址。请先合并或修改引用。";
            return;
        }
        Apply(new DirectoryManagementEdit.DeleteDirectory(SelectedDirectoryRow.Directory));
    }
}
