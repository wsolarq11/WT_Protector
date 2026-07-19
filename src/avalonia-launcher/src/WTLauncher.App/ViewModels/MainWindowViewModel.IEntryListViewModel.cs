using WTLauncher.App.Features.Entries;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryListViewModel implementation — delegates to the extracted
// EntryListViewModel instead of shell-internal methods.
public sealed partial class MainWindowViewModel : IEntryListViewModel
{
    Task IEntryListViewModel.AddEntryAsync() => _entryListViewModel.AddEntryAsync();

    Task IEntryListViewModel.CloneEntryAsync(LauncherEntryViewModel item) =>
        _entryListViewModel.CloneEntryAsync(item);

    Task IEntryListViewModel.EditEntryAsync(LauncherEntryViewModel item) =>
        _entryListViewModel.EditEntryAsync(item);

    Task IEntryListViewModel.DeleteEntryAsync(LauncherEntryViewModel item) =>
        _entryListViewModel.DeleteEntryAsync(item);

    Task IEntryListViewModel.DiscoverAgentsAsync() => DiscoverAgentsAsync();
}
