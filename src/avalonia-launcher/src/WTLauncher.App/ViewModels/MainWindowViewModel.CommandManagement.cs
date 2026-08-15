using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// 命令管理 dialog 集成。Split from MainWindowViewModel.cs (task 07-11-command-management-dialog).
// 使用 IDialogService 打开命令管理 dialog。
// Owner 通过 ApplyCommandManagementEdit 回调执行 snapshot/mutate/save/rollback。
// ponytail: edit 是 sealed record 多态,owner pattern-match 处理。

public sealed partial class MainWindowViewModel
{

    [RelayCommand]
    private async Task OpenCommandManagementAsync()
    {
        DiscardDragReorderPreview();
        try
        {
            await _dialogService.OpenCommandManagementAsync(
                GetCommandRowsSnapshot(),
                GetTemplateRowsSnapshot(),
                this).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.OpenDialog");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("命令管理", ex);
        }
    }

    /// <summary>
    /// 单次 edit 的 transaction: mutate entries/settings → 一次 Commit → 失败恢复。
    /// 成功后同步 _launchOrchestrator.CommandTemplates,让后续启动吃到新模板。
    /// </summary>
    CommandManagementResult ICommandEditApplier.Apply(CommandManagementEdit edit) =>
        ApplyCommandManagementEdit(edit);

    internal CommandManagementResult ApplyCommandManagementEdit(CommandManagementEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        using var transaction = new StoreMutationSession(SaveStoreFile, RestoreVmStateFromStore);
        CommandManagementApplied applied;
        try
        {
            applied = ApplyEditAndReturnStatus(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.Apply");
            return FailureResult($"应用失败: {ExceptionMessageFormatter.ForUser(ex)}");
        }

        var saveFailure = transaction.Commit(applied.StoreFile);
        if (saveFailure is not null)
        {
            // Commit has already reverted ViewModel state; re-sync the runtime
            // template resolver from the StoreGrain that remains authoritative.
            _launchOrchestrator.CommandTemplates = _storeGrain.Select(s => s.Settings.CommandTemplates);
            return FailureResult(saveFailure);
        }

        _launchOrchestrator.CommandTemplates = _storeGrain.Select(s => s.Settings.CommandTemplates);
        RefreshSelectionState();
        ApplyFilter();
        return SuccessResult(applied.StatusMessage);
    }

    private CommandManagementApplied ApplyEditAndReturnStatus(CommandManagementEdit edit)
    {
        switch (edit)
        {
            case CommandManagementEdit.RenameCommand r:
            {
                var oldCmd = r.OldCommand;
                var newCmd = r.NewCommand;
                if (string.Equals(oldCmd, newCmd, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied("新命令与原命令相同,无变更。", BuildStoreFile());

                var matched = 0;
                foreach (var item in _list.MutableEntries)
                {
                    if (string.Equals(item.Command, oldCmd, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newCmd, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                var renameStatus = matched == 0
                    ? $"未找到 command 为 '{oldCmd}' 的 entry。"
                    : $"已将 {matched} 个 entry 的命令从 '{oldCmd}' 改为 '{newCmd}'。";
                return new CommandManagementApplied(renameStatus, BuildStoreFile());
            }

            case CommandManagementEdit.MergeCommands m:
            {
                var loser = m.LoserCommand;
                var keeper = m.KeeperCommand;
                if (string.Equals(loser, keeper, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied("保留方与失效方相同,无法合并。", BuildStoreFile());

                var matched = 0;
                foreach (var item in _list.MutableEntries)
                {
                    if (string.Equals(item.Command, loser, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = keeper, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                var mergeStatus = $"已将 {matched} 个 entry 的命令从 '{loser}' 合并到 '{keeper}'。";
                return new CommandManagementApplied(mergeStatus, BuildStoreFile());
            }

            case CommandManagementEdit.DeleteCommand:
                return new CommandManagementApplied(
                    "已删除未引用的 command (仅清理视图,无 entry 受影响)。",
                    BuildStoreFile());

            case CommandManagementEdit.UpdateTemplateKey uk:
            {
                if (string.Equals(uk.OldKey, uk.NewKey, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied("新 Key 与原 Key 相同,无变更。", BuildStoreFile());

                if (CurrentSettings.CommandTemplates.ContainsKey(uk.NewKey))
                    throw new InvalidOperationException($"Key '{uk.NewKey}' 已存在。");

                var newTemplates = CurrentSettings.CommandTemplates
                    .Remove(uk.OldKey)
                    .Add(uk.NewKey, uk.ExpandedValue);

                // Cascade: entry.Command == "$oldKey" → "$newKey"
                var oldToken = "$" + uk.OldKey;
                var newToken = "$" + uk.NewKey;
                var cascaded = 0;
                foreach (var item in _list.MutableEntries)
                {
                    if (string.Equals(item.Command, oldToken, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newToken, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        cascaded++;
                    }
                }
                var keyStoreFile = BuildStoreFile() with { Settings = CurrentSettings with { CommandTemplates = newTemplates } };
                var keyStatus = $"模板 Key '{uk.OldKey}' → '{uk.NewKey}',自动同步 {cascaded} 个 entry 引用。";
                return new CommandManagementApplied(keyStatus, keyStoreFile);
            }

            case CommandManagementEdit.UpdateTemplateValue uv:
            {
                if (!CurrentSettings.CommandTemplates.ContainsKey(uv.Key))
                    throw new InvalidOperationException($"模板 Key '{uv.Key}' 不存在。");
                var newTemplates = CurrentSettings.CommandTemplates.SetItem(uv.Key, uv.NewExpandedValue);
                var valueStoreFile = BuildStoreFile() with { Settings = CurrentSettings with { CommandTemplates = newTemplates } };
                var valueStatus = $"模板 '{uv.Key}' 展开内容已更新。";
                return new CommandManagementApplied(valueStatus, valueStoreFile);
            }

            case CommandManagementEdit.AddTemplate a:
            {
                if (CurrentSettings.CommandTemplates.ContainsKey(a.Key))
                    throw new InvalidOperationException($"Key '{a.Key}' 已存在。");
                var newTemplates = CurrentSettings.CommandTemplates.Add(a.Key, a.ExpandedValue);
                var addStoreFile = BuildStoreFile() with { Settings = CurrentSettings with { CommandTemplates = newTemplates } };
                var addStatus = $"已添加模板 '{a.Key}'。";
                return new CommandManagementApplied(addStatus, addStoreFile);
            }

            case CommandManagementEdit.DeleteTemplate d:
            {
                if (!CurrentSettings.CommandTemplates.ContainsKey(d.Key))
                    throw new InvalidOperationException($"模板 Key '{d.Key}' 不存在。");
                var newTemplates = CurrentSettings.CommandTemplates.Remove(d.Key);
                var deleteStoreFile = BuildStoreFile() with { Settings = CurrentSettings with { CommandTemplates = newTemplates } };
                var deleteStatus = $"已删除模板 '{d.Key}'。";
                return new CommandManagementApplied(deleteStatus, deleteStoreFile);
            }

            default:
                throw new InvalidOperationException($"未知 edit 类型: {edit.GetType().Name}");
        }
    }

    private IReadOnlyList<CommandRow> GetCommandRowsSnapshot() =>
        CommandUsageAnalyzer.Analyze(_list.MutableEntries.Select(ToLaunchEntry));

    private IReadOnlyList<TemplateRow> GetTemplateRowsSnapshot() =>
        CommandUsageAnalyzer.AnalyzeTemplates(CurrentSettings.CommandTemplates, _list.MutableEntries.Select(ToLaunchEntry));

    private CommandManagementResult SuccessResult(string statusMessage) =>
        new(true, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());

    private CommandManagementResult FailureResult(string statusMessage) =>
        new(false, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());

    private sealed record CommandManagementApplied(string StatusMessage, LauncherStoreFile StoreFile);
}