using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync { get; set; } =
        static _ => Task.FromResult<EntryDraft?>(null);

    public Func<LauncherEntryDeleteTarget, Task<bool>> ConfirmDeleteEntryAsync { get; set; } =
        static _ => Task.FromResult(false);

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
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
        entry.UpdatedAtUtc = CurrentTimestampUtc();
        Entries.Add(new LauncherEntryViewModel(entry, this));
        _settings.CustomEntryOrder.Add(entry.Id);

        if (!SaveEntryCollectionChanges(savePresets: false, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusText = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.EditCanceled(item.Name);
            return;
        }

        var oldId = item.Id;
        var updated = CreateEntryFromDraftOrReportValidation(draft, oldId, item.Entry);
        if (updated is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());
        if (!LauncherEntryIds.Same(oldId, item.Id))
            LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, item.Id);

        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var target = new LauncherEntryDeleteTarget(item.Entry);
        var snapshot = CaptureEntryMutationSnapshot();
        if (!await ConfirmDeleteEntryIfRequiredAsync(target).ConfigureAwait(true))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        Entries.Remove(item);
        LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, target.EntryId);
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
        catch (Exception ex)
        {
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
            Entries.Select(ToLaunchEntry));
        if (validation is null)
            return entry;

        ApplyStatusText(validation);
        return null;
    }

    private bool SaveEntryCollectionChanges(bool savePresets, LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = Entries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            Entries.Select(ToLaunchEntry),
            _configPath,
            _presetFile,
            _presetPath,
            _settings,
            _settingsPath,
            savePresets,
            rollbackSnapshot);

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
            Entries.Select(ToLaunchEntry),
            _presetFile,
            _settings.CustomEntryOrder,
            SelectedPreset?.Id);
    }

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        ReplaceEntries(snapshot.CloneEntries());
        _settings.CustomEntryOrder = snapshot.CloneCustomEntryOrder();
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(Entries);
    }

    private void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = Entries.Count;
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");
}
