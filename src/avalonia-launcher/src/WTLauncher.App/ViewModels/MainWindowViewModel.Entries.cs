using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
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
        var (entry, status) = await _entryService.AddEntryAsync(
            _entries, _settings, CommandTemplates, GetPresetReferenceCount(""), _utcNow).ConfigureAwait(true);

        if (entry is not null)
        {
            var snapshot = CaptureEntryMutationSnapshot();
            _entries.Add(new LauncherEntryViewModel(entry, this));
            _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

            if (!SaveEntryCollectionChanges(snapshot))
                return;

            ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
        }
        else if (status is not null)
        {
            StatusText = status;
        }
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var (entry, status) = await _entryService.CloneEntryAsync(
            item, _entries, _settings, CommandTemplates, GetPresetReferenceCount(item.Id), _utcNow).ConfigureAwait(true);

        if (entry is not null)
        {
            var snapshot = CaptureEntryMutationSnapshot();
            _entries.Add(new LauncherEntryViewModel(entry, this));
            _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

            if (!SaveEntryCollectionChanges(snapshot))
                return;

            ApplyStatusText($"已复制按钮: {item.Name} → {entry.Name}（已保存）");
        }
        else if (status is not null)
        {
            StatusText = status;
        }
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var (updated, status) = await _entryService.EditEntryAsync(
            item, _entries, _presetFile, _settings, CommandTemplates, GetPresetReferenceCount(item.Id), _utcNow).ConfigureAwait(true);

        if (updated is not null)
        {
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
                    StatusText = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
                    return;
                }
            }
            item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());

            if (!SaveEntryCollectionChanges(snapshot))
                return;

            ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
        }
        else if (status is not null)
        {
            StatusText = status;
        }
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var (status, clearFocusId) = await _entryService.DeleteEntryAsync(
            item, _keyboardFocusedEntryId).ConfigureAwait(true);

        if (status is not null)
        {
            StatusText = status;
            return;
        }

        if (clearFocusId is not null)
            _keyboardFocusedEntryId = null;

        var target = new LauncherEntryDeleteTarget(item.SnapshotEntry());
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
            StatusText = saveFailure;
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
