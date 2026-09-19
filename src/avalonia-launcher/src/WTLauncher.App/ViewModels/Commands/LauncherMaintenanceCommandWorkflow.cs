using System.Collections.Immutable;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
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
            _context.Status.Apply(LauncherRefreshStatus.DialogOpenFailed(LauncherLocalization.Format("dialog.commandManagement"), ex));
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
            return FailureResult(LauncherLocalization.Format("maintenance.applyFailed", ExceptionMessageFormatter.ForUser(ex)));
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
            _context.Status.Apply(LauncherRefreshStatus.DialogOpenFailed(LauncherLocalization.Format("dialog.directoryManagement"), ex));
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
            return DirectoryFailure(LauncherLocalization.Format("maintenance.applyFailed", ExceptionMessageFormatter.ForUser(ex)));
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
                    return new CommandManagementApplied(LauncherLocalization.Format("maintenance.renameCommandNoChange"), _host.BuildStoreFile());

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
                    ? LauncherLocalization.Format("maintenance.commandNotFound", oldCmd)
                    : LauncherLocalization.Format("maintenance.commandRenamed", matched, oldCmd, newCmd);
                return new CommandManagementApplied(renameStatus, _host.BuildStoreFile());
            }

            case CommandManagementEdit.MergeCommands m:
            {
                var loser = m.LoserCommand;
                var keeper = m.KeeperCommand;
                if (string.Equals(loser, keeper, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied(LauncherLocalization.Format("maintenance.mergeSame"), _host.BuildStoreFile());

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
                var mergeStatus = LauncherLocalization.Format("maintenance.commandMerged", matched, loser, keeper);
                return new CommandManagementApplied(mergeStatus, _host.BuildStoreFile());
            }

            case CommandManagementEdit.DeleteCommand:
                return new CommandManagementApplied(
                    LauncherLocalization.Format("maintenance.commandDeleted"),
                    _host.BuildStoreFile());

            case CommandManagementEdit.UpdateTemplateKey uk:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (string.Equals(uk.OldKey, uk.NewKey, StringComparison.OrdinalIgnoreCase))
                    return new CommandManagementApplied(LauncherLocalization.Format("maintenance.templateKeyNoChange"), _host.BuildStoreFile());

                if (settings.CommandTemplates.ContainsKey(uk.NewKey))
                    throw new InvalidOperationException(LauncherLocalization.Format("maintenance.keyExists", uk.NewKey));

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
                var keyStatus = LauncherLocalization.Format("maintenance.templateKeyUpdated", uk.OldKey, uk.NewKey, cascaded);
                return new CommandManagementApplied(keyStatus, keyStoreFile);
            }

            case CommandManagementEdit.UpdateTemplateValue uv:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (!settings.CommandTemplates.ContainsKey(uv.Key))
                    throw new InvalidOperationException(LauncherLocalization.Format("maintenance.templateKeyMissing", uv.Key));
                var newTemplates = settings.CommandTemplates.SetItem(uv.Key, uv.NewExpandedValue);
                var valueStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var valueStatus = LauncherLocalization.Format("maintenance.templateValueUpdated", uv.Key);
                return new CommandManagementApplied(valueStatus, valueStoreFile);
            }

            case CommandManagementEdit.AddTemplate a:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (settings.CommandTemplates.ContainsKey(a.Key))
                    throw new InvalidOperationException(LauncherLocalization.Format("maintenance.keyExists", a.Key));
                var newTemplates = settings.CommandTemplates.Add(a.Key, a.ExpandedValue);
                var addStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var addStatus = LauncherLocalization.Format("maintenance.templateAdded", a.Key);
                return new CommandManagementApplied(addStatus, addStoreFile);
            }

            case CommandManagementEdit.DeleteTemplate d:
            {
                var settings = _context.Store.Select(s => s.Settings);
                if (!settings.CommandTemplates.ContainsKey(d.Key))
                    throw new InvalidOperationException(LauncherLocalization.Format("maintenance.templateKeyMissing", d.Key));
                var newTemplates = settings.CommandTemplates.Remove(d.Key);
                var deleteStoreFile = _host.BuildStoreFile() with { Settings = settings with { CommandTemplates = newTemplates } };
                var deleteStatus = LauncherLocalization.Format("maintenance.templateDeleted", d.Key);
                return new CommandManagementApplied(deleteStatus, deleteStoreFile);
            }

            default:
                throw new InvalidOperationException(LauncherLocalization.Format("maintenance.unknownEdit", edit.GetType().Name));
        }
    }

    private string ApplyDirectoryEditAndReturnStatus(DirectoryManagementEdit edit)
    {
        switch (edit)
        {
            case DirectoryManagementEdit.RenameDirectory r:
            {
                if (string.Equals(r.OldDirectory, r.NewDirectory, StringComparison.OrdinalIgnoreCase))
                    return LauncherLocalization.Format("maintenance.directorySame");

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
                    ? LauncherLocalization.Format("maintenance.directoryNotFound", r.OldDirectory)
                    : LauncherLocalization.Format("maintenance.directoryRenamed", matched, r.NewDirectory);
            }

            case DirectoryManagementEdit.MergeDirectories m:
            {
                if (string.Equals(m.LoserDirectory, m.KeeperDirectory, StringComparison.OrdinalIgnoreCase))
                    return LauncherLocalization.Format("maintenance.mergeSame");

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
                return LauncherLocalization.Format("maintenance.directoryMerged", matched, m.KeeperDirectory);
            }

            case DirectoryManagementEdit.DeleteDirectory:
                return LauncherLocalization.Format("maintenance.directoryDeleted");

            default:
                throw new InvalidOperationException(LauncherLocalization.Format("maintenance.unknownEdit", edit.GetType().Name));
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
