using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

// Entry Add/Clone/Edit/Delete command flow. Split from MainWindowViewModel.cs as
// part of 07-09-split-mainviewmodel-stage2. Uses IEntryService for command logic.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task AddEntryAsync()
    {
        await _entryManager.EntryListViewModel.AddEntryAsync();
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        await _entryManager.EntryListViewModel.CloneEntryAsync(item);
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        await _entryManager.EntryListViewModel.EditEntryAsync(item);
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        await _entryManager.EntryListViewModel.DeleteEntryAsync(item);
    }

    private bool SaveEntryCollectionChanges(LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = _entryManager.MutableEntries.Count;
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

        RefreshAllProjections();
        return true;
    }

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherEntryMutationSnapshot.Capture(
            _entryManager.MutableEntries.Select(ToLaunchEntry),
            _presetManager.File,
            _settings,
            SelectedPreset?.Id);
    }

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        ReplaceEntries(snapshot.CloneEntries());
        _settings = snapshot.CloneSettings();
        _presetManager.Load(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entryManager.MutableEntries);
    }
}
