using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;

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

    private bool SaveEntryCollectionChanges()
    {
        TotalCount = _list.MutableEntries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is null)
        {
            RefreshAllProjections();
        }
        else
        {
            RestoreVmStateFromStore();
            StatusContent = LauncherStatusContent.Error(saveFailure);
        }
        return saveFailure is null;
    }
}
