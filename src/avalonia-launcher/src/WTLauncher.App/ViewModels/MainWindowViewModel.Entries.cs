using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

// Entry Add/Clone/Edit/Delete command flow. Split from MainWindowViewModel.cs as
// part of 07-09-split-mainviewmodel-stage2. Uses IEntryService for command logic.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task AddEntryAsync()
    {
        await _list.AddEntryAsync();
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        await _list.CloneEntryAsync(item);
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        await _list.EditEntryAsync(item);
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        await _list.DeleteEntryAsync(item);
    }

    private bool SaveEntryCollectionChanges(LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = _list.MutableEntries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
            RestoreEntryMutationSnapshot(rollbackSnapshot);
        else
            RefreshAllProjections();

        if (saveFailure is not null)
            StatusContent = LauncherStatusContent.Error(saveFailure);
        return saveFailure is null;
    }

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherEntryMutationSnapshot.Capture(
            _list.MutableEntries.Select(ToLaunchEntry),
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
        LauncherEntryLayoutPresentation.Refresh(_list.MutableEntries);
    }
}
