using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync { get; set; } =
        static _ => Task.FromResult<EntryDraft?>(null);

    public Func<LauncherEntryViewModel, Task<bool>> ConfirmDeleteEntryAsync { get; set; } =
        static _ => Task.FromResult(false);

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
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
        var draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
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
        if (!await ConfirmDeleteEntryIfRequiredAsync(item).ConfigureAwait(true))
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        var entryName = item.Name;
        Entries.Remove(item);
        LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, item.Id);
        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Deleted(entryName));
    }

    private async Task<bool> ConfirmDeleteEntryIfRequiredAsync(LauncherEntryViewModel item)
    {
        var accepted = await ConfirmDeleteEntryAsync(item).ConfigureAwait(true);
        if (accepted)
            return true;

        StatusText = LauncherEntryStatus.DeleteCanceled(item.Name);
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

        var saveFailure = LauncherEntryMutationPersistence.Save(
            Entries.Select(ToLaunchEntry),
            _configPath,
            _presetFile,
            _presetPath,
            _settings,
            _settingsPath,
            savePresets,
            rollbackSnapshot,
            EnsureCustomOrderContainsEntries);

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

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot() =>
        LauncherEntryMutationSnapshot.Capture(
            Entries.Select(ToLaunchEntry),
            _presetFile,
            _settings.CustomEntryOrder,
            SelectedPreset?.Id);

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
