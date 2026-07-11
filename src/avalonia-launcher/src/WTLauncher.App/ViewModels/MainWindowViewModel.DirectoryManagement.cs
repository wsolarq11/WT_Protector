using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// 地址管理 dialog 集成。Split from MainWindowViewModel.cs (task 07-11-directory-management-dialog).
// 与 CommandManagement 同模式: Func 注入 dialog,owner 提供 Apply 回调做 snapshot/mutate/save/rollback。

public sealed partial class MainWindowViewModel
{
    private Func<Func<DirectoryManagementEdit, DirectoryManagementResult>, Task> _openDirectoryManagementDialog =
        static _ => Task.CompletedTask;

    public Func<Func<DirectoryManagementEdit, DirectoryManagementResult>, Task> OpenDirectoryManagementDialog
    {
        get => _openDirectoryManagementDialog;
        set => _openDirectoryManagementDialog = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand]
    private async Task OpenDirectoryManagementAsync()
    {
        DiscardDragReorderPreview();
        try
        {
            await OpenDirectoryManagementDialog(ApplyDirectoryManagementEdit).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.OpenDialog");
            StatusText = $"地址管理对话框打开失败: {ExceptionMessageFormatter.ForUser(ex)}";
        }
    }

    internal DirectoryManagementResult ApplyDirectoryManagementEdit(DirectoryManagementEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        var snapshot = CaptureEntryMutationSnapshot();
        string statusMessage;
        try
        {
            statusMessage = ApplyEditAndReturnStatus(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Apply");
            RestoreEntryMutationSnapshot(snapshot);
            return DirectoryFailure($"应用失败: {ExceptionMessageFormatter.ForUser(ex)}");
        }

        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            RestoreEntryMutationSnapshot(snapshot);
            return DirectoryFailure(saveFailure);
        }

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
                foreach (var item in _entries)
                {
                    if (string.Equals(item.Directory, r.OldDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = r.NewDirectory, UpdatedAtUtc = CurrentTimestampUtc() };
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
                foreach (var item in _entries)
                {
                    if (string.Equals(item.Directory, m.LoserDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = m.KeeperDirectory, UpdatedAtUtc = CurrentTimestampUtc() };
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
        DirectoryUsageAnalyzer.Analyze(_entries.Select(ToLaunchEntry));

    private DirectoryManagementResult DirectorySuccess(string statusMessage) =>
        new(true, statusMessage, GetDirectoryRowsSnapshot());

    private DirectoryManagementResult DirectoryFailure(string statusMessage) =>
        new(false, statusMessage, GetDirectoryRowsSnapshot());
}
