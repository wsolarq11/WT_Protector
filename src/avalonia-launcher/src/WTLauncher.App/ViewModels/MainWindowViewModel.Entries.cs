using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Func<LaunchEntry?, Task<EntryDraft?>> _requestEntryDraftAsync =
        static _ => Task.FromResult<EntryDraft?>(null);

    private Func<LauncherEntryDeleteTarget, Task<bool>> _confirmDeleteEntryAsync =
        static _ => Task.FromResult(false);

    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync
    {
        get => _requestEntryDraftAsync;
        set => _requestEntryDraftAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Func<LauncherEntryDeleteTarget, Task<bool>> ConfirmDeleteEntryAsync
    {
        get => _confirmDeleteEntryAsync;
        set => _confirmDeleteEntryAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddEntry");
            StatusText = LauncherEntryStatus.AddDialogFailed(ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.AddCanceled;
            return;
        }

        var entry = CreateEntryFromDraftOrReportValidation(
            draft,
            originalId: null,
            previousEntry: null);
        if (entry is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
        _entries.Add(new LauncherEntryViewModel(entry, this));
        _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

        if (!SaveEntryCollectionChanges(savePresets: false, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var source = item.SnapshotEntry();
        var cloneSource = new LaunchEntry
        {
            Id = GenerateUniqueCloneId(source.Id, _entries.Select(e => e.Id)),
            Name = source.Name + " (副本)",
            Directory = source.Directory,
            Command = source.Command,
            Selected = false,
        };

        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(cloneSource).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CloneEntry");
            StatusText = LauncherEntryStatus.AddDialogFailed(ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.AddCanceled;
            return;
        }

        var entry = CreateEntryFromDraftOrReportValidation(
            draft,
            originalId: null,
            previousEntry: null);
        if (entry is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
        _entries.Add(new LauncherEntryViewModel(entry, this));
        _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

        if (!SaveEntryCollectionChanges(savePresets: false, snapshot))
            return;

        ApplyStatusText($"已复制按钮: {source.Name} → {entry.Name}（已保存）");
    }

    public static string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds)
    {
        var baseId = sourceId + "-copy";
        var candidate = baseId;
        var index = 2;
        var idSet = existingIds.ToHashSet(LauncherEntryIds.Comparer);
        while (idSet.Contains(candidate))
        {
            if (index > 10000)
                throw new InvalidOperationException("Cannot generate a unique clone ID — too many copies exist.");
            candidate = $"{baseId}-{index++}";
        }
        return candidate;
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(item.SnapshotEntry()).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "EditEntry.Draft");
            StatusText = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.EditCanceled(item.Name);
            return;
        }

        var oldId = item.Id;
        var updated = CreateEntryFromDraftOrReportValidation(draft, oldId, item.SnapshotEntry());
        if (updated is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());
        if (!LauncherEntryIds.Same(oldId, item.Id))
        {
            try
            {
                var (updatedPresetFile, updatedOrder, _) = LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, item.Id);
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

        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var target = new LauncherEntryDeleteTarget(item.SnapshotEntry());
        if (!await ConfirmDeleteEntryIfRequiredAsync(target).ConfigureAwait(true))
            return;

        // Clear keyboard focus before removal so _keyboardFocusedEntryId does not
        // retain a stale reference to the deleted entry.
        if (LauncherEntryIds.Same(_keyboardFocusedEntryId, target.EntryId))
            KeyboardFocusedEntryId = null;

        var snapshot = CaptureEntryMutationSnapshot();
        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, target.EntryId);
        _presetFile = updatedPresetFile;
        _settings = _settings with { CustomEntryOrder = updatedOrder };
        _entries.Remove(item);
        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Deleted(target.EntryName));
    }

    private async Task<bool> ConfirmDeleteEntryIfRequiredAsync(LauncherEntryDeleteTarget target)
    {
        bool accepted;
        try
        {
            accepted = await ConfirmDeleteEntryAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeleteConfirmation");
            StatusText = LauncherEntryStatus.DeleteConfirmationFailed(target.EntryName, ex);
            return false;
        }

        if (accepted)
            return true;

        StatusText = LauncherEntryStatus.DeleteCanceled(target.EntryName);
        return false;
    }

    private LaunchEntry? CreateEntryFromDraftOrReportValidation(
        EntryDraft draft,
        string? originalId,
        LaunchEntry? previousEntry)
    {
        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry);
        var validation = LauncherEntryDrafts.ValidateEntry(
            entry,
            originalId,
            _entries.Select(ToLaunchEntry));
        if (validation is null)
            return entry;

        ApplyStatusText(validation);
        return null;
    }

    private bool SaveEntryCollectionChanges(bool savePresets, LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = _entries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = SaveStoreFile(BuildStoreFile());

        if (saveFailure is not null)
        {
            StatusText = saveFailure;
            RestoreEntryMutationSnapshot(rollbackSnapshot);
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

    private void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
        {
            _entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = _entries.Count;
        _keyboardFocusedEntryId = null;
        OnPropertyChanged(nameof(KeyboardFocusedEntryId));

        // Force card-size recalculation on next ApplyFilter — new entry VMs
        // start with default dimensions and must be measured against current available size.
        _lastAppliedCardsAvailableSize = new Avalonia.Size(0, 0);
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");
}
