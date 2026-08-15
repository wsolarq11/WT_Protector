using System.Collections.Immutable;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Maintenance command workflow: command/template and directory batch edits.
/// Each edit is one StoreMutationSession commit with rollback on save failure.
/// </summary>
public sealed partial class LauncherMaintenanceCommandWorkflow : ICommandEditApplier, IDirectoryEditApplier
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly IDialogService _dialogService;
    private readonly LaunchOrchestrator _launch;
    private readonly ICommandHost _host;

    public LauncherMaintenanceCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        IDialogService dialogService,
        LaunchOrchestrator launch,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public async Task OpenCommandManagementAsync()
    {
        _host.DiscardDragReorderPreview();
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
            _context.Status.Apply(LauncherRefreshStatus.DialogOpenFailed("命令管理", ex));
        }
    }

    CommandManagementResult ICommandEditApplier.Apply(CommandManagementEdit edit) =>
        ApplyCommandManagementEdit(edit);

    public CommandManagementResult ApplyCommandManagementEdit(CommandManagementEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        using var transaction = new StoreMutationSession(_host.SaveStoreFile, _host.RestoreVmStateFromStore);
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
            _launch.CommandTemplates = _context.Store.Select(s => s.Settings.CommandTemplates);
            return FailureResult(saveFailure);
        }

        _launch.CommandTemplates = _context.Store.Select(s => s.Settings.CommandTemplates);
        _host.RefreshSelectionState();
        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        return SuccessResult(applied.StatusMessage);
    }

    public async Task OpenDirectoryManagementAsync()
    {
        _host.DiscardDragReorderPreview();
        try
        {
            await _dialogService.OpenDirectoryManagementAsync(
                GetDirectoryRowsSnapshot(),
                this).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.OpenDialog");
            _context.Status.Apply(LauncherRefreshStatus.DialogOpenFailed("地址管理", ex));
        }
    }

    DirectoryManagementResult IDirectoryEditApplier.Apply(DirectoryManagementEdit edit) =>
        ApplyDirectoryManagementEdit(edit);

    public DirectoryManagementResult ApplyDirectoryManagementEdit(DirectoryManagementEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);

        using var transaction = new StoreMutationSession(_host.SaveStoreFile, _host.RestoreVmStateFromStore);
        string statusMessage;
        try
        {
            statusMessage = ApplyDirectoryEditAndReturnStatus(edit);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Apply");
            return DirectoryFailure($"应用失败: {ExceptionMessageFormatter.ForUser(ex)}");
        }

        var saveFailure = transaction.Commit(_host.BuildStoreFile());
        if (saveFailure is not null)
            return DirectoryFailure(saveFailure);

        _context.SynchronizeProjections(ProjectionSyncMode.Mutation);
        return DirectorySuccess(statusMessage);
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
                    return new CommandManagementApplied("新命令与原命令相同,无变更。", _host.BuildStoreFile());

                var matched = 0;
                foreach (var item in _entries.MutableEntries)
                {
                    if (string.Equals(item.Command, oldCmd, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newCmd, UpdatedAtUtc = _context.GetTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                var renameStatus = matched == 0
                    ? $"未找到 command 为 '{oldCmd}' 的 entry。"
                    : $"已将 {matched} 个 entry 的命令从 '{oldCmd}' 改为 '{newCmd}'。";
                return new CommandManagementApplied(renameStatus, _host.BuildStoreFile());
            }

            case CommandManagementEdit.MergeCommands m:
            {
                var loser = m.LoserCommand;
                var keeper = m.KeeperCommand;
                if (string.Equals(loser, keeper, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied("保留方与失效方相同,无法合并。", _host.BuildStoreFile());

                var matched = 0;
                foreach (var item in _entries.MutableEntries)
                {
                    if (string.Equals(item.Command, loser, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = keeper, UpdatedAtUtc = _context.GetTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        matched++;
                    }
                }
                var mergeStatus = $"已将 {matched} 个 entry 的命令从 '{loser}' 合并到 '{keeper}'。";
                return new CommandManagementApplied(mergeStatus, _host.BuildStoreFile());
            }

            case CommandManagementEdit.DeleteCommand:
                return new CommandManagementApplied(
                    "已删除未引用的 command (仅清理视图,无 entry 受影响)。",
                    _host.BuildStoreFile());

            case CommandManagementEdit.UpdateTemplateKey uk:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (string.Equals(uk.OldKey, uk.NewKey, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied("新 Key 与原 Key 相同,无变更。", _host.BuildStoreFile());

                if (settings.CommandTemplates.ContainsKey(uk.NewKey))
                    throw new InvalidOperationException($"Key '{uk.NewKey}' 已存在。");

                var newTemplates = settings.CommandTemplates
                    .Remove(uk.OldKey)
                    .Add(uk.NewKey, uk.ExpandedValue);

                var oldToken = "$" + uk.OldKey;
                var newToken = "$" + uk.NewKey;
                var cascaded = 0;
                foreach (var item in _entries.MutableEntries)
                {
                    if (string.Equals(item.Command, oldToken, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Command = newToken, UpdatedAtUtc = _context.GetTimestampUtc() };
                        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
                        cascaded++;
                    }
                }
                var keyStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var keyStatus = $"模板 Key '{uk.OldKey}' → '{uk.NewKey}',自动同步 {cascaded} 个 entry 引用。";
                return new CommandManagementApplied(keyStatus, keyStoreFile);
            }

            case CommandManagementEdit.UpdateTemplateValue uv:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (!settings.CommandTemplates.ContainsKey(uv.Key))
                    throw new InvalidOperationException($"模板 Key '{uv.Key}' 不存在。");
                var newTemplates = settings.CommandTemplates.SetItem(uv.Key, uv.NewExpandedValue);
                var valueStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var valueStatus = $"模板 '{uv.Key}' 展开内容已更新。";
                return new CommandManagementApplied(valueStatus, valueStoreFile);
            }

            case CommandManagementEdit.AddTemplate a:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (settings.CommandTemplates.ContainsKey(a.Key))
                    throw new InvalidOperationException($"Key '{a.Key}' 已存在。");
                var newTemplates = settings.CommandTemplates.Add(a.Key, a.ExpandedValue);
                var addStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var addStatus = $"已添加模板 '{a.Key}'。";
                return new CommandManagementApplied(addStatus, addStoreFile);
            }

            case CommandManagementEdit.DeleteTemplate d:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (!settings.CommandTemplates.ContainsKey(d.Key))
                    throw new InvalidOperationException($"模板 Key '{d.Key}' 不存在。");
                var newTemplates = settings.CommandTemplates.Remove(d.Key);
                var deleteStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var deleteStatus = $"已删除模板 '{d.Key}'。";
                return new CommandManagementApplied(deleteStatus, deleteStoreFile);
            }

            default:
                throw new InvalidOperationException($"未知 edit 类型: {edit.GetType().Name}");
        }
    }

    private string ApplyDirectoryEditAndReturnStatus(DirectoryManagementEdit edit)
    {
        switch (edit)
        {
            case DirectoryManagementEdit.RenameDirectory r:
            {
                if (string.Equals(r.OldDirectory, r.NewDirectory, StringComparison.OrdinalIgnoreCase))
                    return "新地址与原地址相同,无变更。";

                var matched = 0;
                foreach (var item in _entries.MutableEntries)
                {
                    if (string.Equals(item.Directory, r.OldDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = (DirectoryPath)r.NewDirectory, UpdatedAtUtc = _context.GetTimestampUtc() };
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
                foreach (var item in _entries.MutableEntries)
                {
                    if (string.Equals(item.Directory, m.LoserDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        var updated = item.SnapshotEntry() with { Directory = (DirectoryPath)m.KeeperDirectory, UpdatedAtUtc = _context.GetTimestampUtc() };
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

    private IReadOnlyList<CommandRow> GetCommandRowsSnapshot() =>
        CommandUsageAnalyzer.Analyze(_entries.MutableEntries.Select(ToLaunchEntry));

    private IReadOnlyList<TemplateRow> GetTemplateRowsSnapshot() =>
        CommandUsageAnalyzer.AnalyzeTemplates(
            _context.Store.Select(s => s.Settings.CommandTemplates),
            _entries.MutableEntries.Select(ToLaunchEntry));

    private IReadOnlyList<DirectoryRow> GetDirectoryRowsSnapshot() =>
        DirectoryUsageAnalyzer.Analyze(_entries.MutableEntries.Select(ToLaunchEntry));

    private CommandManagementResult SuccessResult(string statusMessage) =>
        new(true, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());

    private CommandManagementResult FailureResult(string statusMessage) =>
        new(false, statusMessage, GetCommandRowsSnapshot(), GetTemplateRowsSnapshot());

    private DirectoryManagementResult DirectorySuccess(string statusMessage) =>
        new(true, statusMessage, GetDirectoryRowsSnapshot());

    private DirectoryManagementResult DirectoryFailure(string statusMessage) =>
        new(false, statusMessage, GetDirectoryRowsSnapshot());

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private sealed record CommandManagementApplied(string StatusMessage, LauncherStoreFile StoreFile);
}
