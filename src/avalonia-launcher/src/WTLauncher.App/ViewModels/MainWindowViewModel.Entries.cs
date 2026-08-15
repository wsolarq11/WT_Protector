using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

// Entry Add/Clone/Edit/Delete command surface. The actual add/clone/edit/delete
// flows live in EntryListViewModel; collection persistence lives in
// LauncherEntryCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task AddEntryAsync() => _list.AddEntryAsync();

    internal Task CloneEntryAsync(LauncherEntryViewModel item) =>
        _list.CloneEntryAsync(item);

    internal Task EditEntryAsync(LauncherEntryViewModel item) =>
        _list.EditEntryAsync(item);

    internal Task DeleteEntryAsync(LauncherEntryViewModel item) =>
        _list.DeleteEntryAsync(item);

    private bool SaveEntryCollectionChanges() => _entryWorkflow.SaveEntryCollectionChanges();
}
