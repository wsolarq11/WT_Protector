using WTLauncher.App.Features.Entries;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryListViewModel implementation — delegates to the extracted
// EntryListViewModel instead of shell-internal methods.
public sealed partial class MainWindowViewModel : IEntryListViewModel
{
    Task IEntryListViewModel.AddEntryAsync() => _entryManager.EntryListViewModel.AddEntryAsync();

    Task IEntryListViewModel.CloneEntryAsync(LauncherEntryViewModel item) =>
        _entryManager.EntryListViewModel.CloneEntryAsync(item);

    Task IEntryListViewModel.EditEntryAsync(LauncherEntryViewModel item) =>
        _entryManager.EntryListViewModel.EditEntryAsync(item);

    Task IEntryListViewModel.DeleteEntryAsync(LauncherEntryViewModel item) =>
        _entryManager.EntryListViewModel.DeleteEntryAsync(item);

    Task IEntryListViewModel.DiscoverAgentsAsync() => DiscoverAgentsAsync();
}
