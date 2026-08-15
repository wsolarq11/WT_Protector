using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// 地址管理 dialog 集成。Split from MainWindowViewModel.cs (task 07-11-directory-management-dialog).
// 使用 IDialogService 打开地址管理 dialog。
// Owner 通过 Apply 回调做 snapshot/mutate/save/rollback。

public sealed partial class MainWindowViewModel
{

    [RelayCommand]
    private async Task OpenDirectoryManagementAsync()
    {
        DiscardDragReorderPreview();
        try
        {
            await _dialogService.OpenDirectoryManagementAsync(
                GetDirectoryRowsSnapshot(),
                ApplyDirectoryManagementEdit).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.OpenDialog");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("地址管理", ex);
        }
    }

    internal DirectoryManagementResult ApplyDirectoryManagementEdit(DirectoryManagementEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        using var transaction = new StoreMutationSession(SaveStoreFile, RestoreVmStateFromStore);
        string statusMessage;
        try
        {
            statusMessage = ApplyEditAndReturnStatus(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Apply");
            return DirectoryFailure($"应用失败: {ExceptionMessageFormatter.ForUser(ex)}");
        }

        var saveFailure = transaction.Commit(BuildStoreFile());
        if (saveFailure is not null)
            return DirectoryFailure(saveFailure);

        RefreshSelectionState();
        ApplyFilter();
        return DirectorySuccess(statusMessage);
    }

    private string ApplyEditAndReturnStatus(DirectoryManagementEdit edit)
    {
        switch (edit)
        {
            case DirectoryManagementEdit.RenameDirectory r:
            {
                if (string.Equals(r.OldDirectory, r.NewDirectory, StringComparison.OrdinalIgnoreCase))
                    return "新地址与原地址相同,无变更。";

                var matched = 0;
                foreach (var item in _list.MutableEntries)
                {
                    if (string.Equals(item.Directory, r.OldDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = (DirectoryPath)r.NewDirectory, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                return matched == 0
                    ? $"未找到 directory 为 '{r.OldDirectory}' 的 entry。"
                    : $"已将 {matched} 个 entry 的地址改为 '{r.NewDirectory}'。";
            }

            case DirectoryManagementEdit.MergeDirectories m:
            {
                if (string.Equals(m.LoserDirectory, m.KeeperDirectory, StringComparison.OrdinalIgnoreCase))
                    return "保留方与失效方相同,无法合并。";

                var matched = 0;
                foreach (var item in _list.MutableEntries)
                {
                    if (string.Equals(item.Directory, m.LoserDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = (DirectoryPath)m.KeeperDirectory, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                return $"已将 {matched} 个 entry 的地址合并到 '{m.KeeperDirectory}'。";
            }

            case DirectoryManagementEdit.DeleteDirectory:
                return "已删除未引用的地址(仅清理视图,无 entry 受影响)。";

            default:
                throw new InvalidOperationException($"未知 edit 类型: {edit.GetType().Name}");
        }
    }

    private IReadOnlyList<DirectoryRow> GetDirectoryRowsSnapshot() =>
        DirectoryUsageAnalyzer.Analyze(_list.MutableEntries.Select(ToLaunchEntry));

    private DirectoryManagementResult DirectorySuccess(string statusMessage) =>
        new(true, statusMessage, GetDirectoryRowsSnapshot());

    private DirectoryManagementResult DirectoryFailure(string statusMessage) =>
        new(false, statusMessage, GetDirectoryRowsSnapshot());
}
