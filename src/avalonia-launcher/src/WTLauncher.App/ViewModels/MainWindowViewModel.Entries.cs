using System.Collections.Immutable;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Entry Add/Clone/Edit/Delete command flow. Split from MainWindowViewModel.cs as
// part of 07-09-split-mainviewmodel-stage2. Uses IEntryService for command logic.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task AddEntryAsync()
    {
        DiscardDragReorderPreview();
        var result = await _entryService.AddEntryAsync(
            _entries, _settings, CommandTemplates, GetPresetReferenceCount(""), _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var entry = success.Value;
                var snapshot = CaptureEntryMutationSnapshot();
                _entries.Add(new LauncherEntryViewModel(entry, this));
                _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

                if (!SaveEntryCollectionChanges(snapshot))
                    return;

                ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusContent = canceled.Status;
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusContent = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var result = await _entryService.CloneEntryAsync(
            item, _entries, _settings, CommandTemplates, GetPresetReferenceCount(item.Id), _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var entry = success.Value;
                var snapshot = CaptureEntryMutationSnapshot();
                _entries.Add(new LauncherEntryViewModel(entry, this));
                _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

                if (!SaveEntryCollectionChanges(snapshot))
                    return;

                ApplyStatusText($"已复制按钮: {item.Name} → {entry.Name}（已保存）");
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusContent = canceled.Status;
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusContent = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var result = await _entryService.EditEntryAsync(
            item, _entries, _presetFile, _settings, CommandTemplates, GetPresetReferenceCount(item.Id), _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var updated = success.Value;
                var oldId = item.Id;
                var snapshot = CaptureEntryMutationSnapshot();
                if (!EntryId.Same(oldId, updated.Id))
                {
                    try
                    {
                        var (updatedPresetFile, updatedOrder, _) = LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, updated.Id);
                        _presetFile = updatedPresetFile;
                        _settings = _settings with { CustomEntryOrder = updatedOrder };
                    }
                    catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
                    {
                        ErrorLog.Write(ex, "EditEntry.RenameReference");
                        RestoreEntryMutationSnapshot(snapshot);
                        StatusContent = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
                        return;
                    }
                }
                item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());

                if (!SaveEntryCollectionChanges(snapshot))
                    return;

                ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusContent = canceled.Status;
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusContent = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var result = await _entryService.DeleteEntryAsync(
            item, _keyboardFocusedEntryId).ConfigureAwait(true);
        EntryDeleteApproval approval;
        switch (result)
        {
            case ServiceSuccess<EntryDeleteApproval> success:
                approval = success.Value;
                break;
            case ServiceCanceled<EntryDeleteApproval> canceled:
                StatusContent = canceled.Status;
                return;
            case ServiceFailure<EntryDeleteApproval> failure:
                StatusContent = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }

        if (approval.ClearKeyboardFocusId is not null)
            _keyboardFocusedEntryId = null;

        var target = approval.Target;
        var snapshot = CaptureEntryMutationSnapshot();
        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, target.EntryId);
        _presetFile = updatedPresetFile;
        _settings = _settings with { CustomEntryOrder = updatedOrder };
        _entries.Remove(item);
        if (!SaveEntryCollectionChanges(snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Deleted(target.EntryName));
    }

    private bool SaveEntryCollectionChanges(LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = _entries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = LauncherMutationTransaction.Commit(
            rollbackSnapshot,
            () => SaveStoreFile(BuildStoreFile()),
            RestoreEntryMutationSnapshot);

        if (saveFailure is not null)
        {
            StatusContent = LauncherStatusContent.Error(saveFailure);
            return false;
        }

        RefreshSelectionState();
        ApplyFilter();
        return true;
    }

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherEntryMutationSnapshot.Capture(
            _entries.Select(ToLaunchEntry),
            _presetFile,
            _settings,
            SelectedPreset?.Id);
    }

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        ReplaceEntries(snapshot.CloneEntries());
        _settings = snapshot.CloneSettings();
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
    }
}
