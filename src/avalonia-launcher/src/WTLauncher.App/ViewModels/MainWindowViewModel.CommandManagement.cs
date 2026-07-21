using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
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
                ApplyCommandManagementEdit).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.OpenDialog");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("命令管理", ex);
        }
    }

    /// <summary>
    /// 单次 edit 的 transaction: snapshot → mutate (entries + settings.CommandTemplates) → save → rollback on failure。
    /// 成功后同步 _launchOrchestrator.CommandTemplates,让后续启动吃到新模板。
    /// </summary>
    internal CommandManagementResult ApplyCommandManagementEdit(CommandManagementEdit edit)
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
            ErrorLog.Write(ex, "CommandManagement.Apply");
            RestoreEntryMutationSnapshot(snapshot);
            return FailureResult($"应用失败: {ExceptionMessageFormatter.ForUser(ex)}");
        }

        // Sync runtime template resolver with new _settings.CommandTemplates.
        // ponytail: pre-existing latent coupling — _launchOrchestrator.CommandTemplates was set once at ctor;
        // command management is the first runtime mutation path, so it must keep this in sync.
        _launchOrchestrator.CommandTemplates = _settings.CommandTemplates;

        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            RestoreEntryMutationSnapshot(snapshot);
            // restore also reverts _launchWorkflow snapshot — re-sync after rollback.
            _launchOrchestrator.CommandTemplates = _settings.CommandTemplates;
            return FailureResult(saveFailure);
        }

        RefreshSelectionState();
        ApplyFilter();
        return SuccessResult(statusMessage);
    }

    private string ApplyEditAndReturnStatus(CommandManagementEdit edit)
    {
        switch (edit)
        {
            case CommandManagementEdit.RenameCommand r:
            {
                var oldCmd = r.OldCommand;
                var newCmd = r.NewCommand;
                if (string.Equals(oldCmd, newCmd, StringComparison.OrdinalIgnoreCase))
                    return "新命令与原命令相同,无变更。";

                var matched = 0;
                foreach (var item in _entryManager.MutableEntries)
                {
                    if (string.Equals(item.Command, oldCmd, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newCmd, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                return matched == 0
                    ? $"未找到 command 为 '{oldCmd}' 的 entry。"
                    : $"已将 {matched} 个 entry 的命令从 '{oldCmd}' 改为 '{newCmd}'。";
            }

            case CommandManagementEdit.MergeCommands m:
            {
                var loser = m.LoserCommand;
                var keeper = m.KeeperCommand;
                if (string.Equals(loser, keeper, StringComparison.OrdinalIgnoreCase))
                    return "保留方与失效方相同,无法合并。";

                var matched = 0;
                foreach (var item in _entryManager.MutableEntries)
                {
                    if (string.Equals(item.Command, loser, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = keeper, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                return $"已将 {matched} 个 entry 的命令从 '{loser}' 合并到 '{keeper}'。";
            }

            case CommandManagementEdit.DeleteCommand:
                return "已删除未引用的 command (仅清理视图,无 entry 受影响)。";

            case CommandManagementEdit.UpdateTemplateKey uk:
            {
                if (string.Equals(uk.OldKey, uk.NewKey, StringComparison.OrdinalIgnoreCase))
                    return "新 Key 与原 Key 相同,无变更。";

                if (_settings.CommandTemplates.ContainsKey(uk.NewKey))
                    throw new InvalidOperationException($"Key '{uk.NewKey}' 已存在。");

                var newTemplates = _settings.CommandTemplates
                    .Remove(uk.OldKey)
                    .Add(uk.NewKey, uk.ExpandedValue);

                // Cascade: entry.Command == "$oldKey" → "$newKey"
                var oldToken = "$" + uk.OldKey;
                var newToken = "$" + uk.NewKey;
                var cascaded = 0;
                foreach (var item in _entryManager.MutableEntries)
                {
                    if (string.Equals(item.Command, oldToken, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newToken, UpdatedAtUtc = CurrentTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        cascaded++;
                    }
                }
                _settings = _settings with { CommandTemplates = newTemplates };
                return $"模板 Key '{uk.OldKey}' → '{uk.NewKey}',自动同步 {cascaded} 个 entry 引用。";
            }

            case CommandManagementEdit.UpdateTemplateValue uv:
            {
                if (!_settings.CommandTemplates.ContainsKey(uv.Key))
                    throw new InvalidOperationException($"模板 Key '{uv.Key}' 不存在。");
                var newTemplates = _settings.CommandTemplates.SetItem(uv.Key, uv.NewExpandedValue);
                _settings = _settings with { CommandTemplates = newTemplates };
                return $"模板 '{uv.Key}' 展开内容已更新。";
            }

            case CommandManagementEdit.AddTemplate a:
            {
                if (_settings.CommandTemplates.ContainsKey(a.Key))
                    throw new InvalidOperationException($"Key '{a.Key}' 已存在。");
                var newTemplates = _settings.CommandTemplates.Add(a.Key, a.ExpandedValue);
                _settings = _settings with { CommandTemplates = newTemplates };
                return $"已添加模板 '{a.Key}'。";
            }

            case CommandManagementEdit.DeleteTemplate d:
            {
                if (!_settings.CommandTemplates.ContainsKey(d.Key))
                    throw new InvalidOperationException($"模板 Key '{d.Key}' 不存在。");
                var newTemplates = _settings.CommandTemplates.Remove(d.Key);
                _settings = _settings with { CommandTemplates = newTemplates };
                return $"已删除模板 '{d.Key}'。";
            }

            default:
                throw new InvalidOperationException($"未知 edit 类型: {edit.GetType().Name}");
        }
    }

    private IReadOnlyList<CommandRow> GetCommandRowsSnapshot() =>
        CommandUsageAnalyzer.Analyze(_entryManager.MutableEntries.Select(ToLaunchEntry));

    private IReadOnlyList<TemplateRow> GetTemplateRowsSnapshot() =>
        CommandUsageAnalyzer.AnalyzeTemplates(_settings.CommandTemplates, _entryManager.MutableEntries.Select(ToLaunchEntry));

    private CommandManagementResult SuccessResult(string statusMessage) =>
        new(true, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());

    private CommandManagementResult FailureResult(string statusMessage) =>
        new(false, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());
}
