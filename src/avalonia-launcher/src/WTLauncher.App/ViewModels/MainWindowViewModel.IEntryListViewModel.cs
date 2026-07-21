using WTLauncher.App.Features.Entries;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryListViewModel implementation — delegates to the extracted
// EntryListViewModel instead of shell-internal methods.
public sealed partial class MainWindowViewModel : IEntryListViewModel
{
    Task IEntryListViewModel.AddEntryAsync() => _list.AddEntryAsync();

    Task IEntryListViewModel.CloneEntryAsync(LauncherEntryViewModel item) =>
        _list.CloneEntryAsync(item);

    Task IEntryListViewModel.EditEntryAsync(LauncherEntryViewModel item) =>
        _list.EditEntryAsync(item);

    Task IEntryListViewModel.DeleteEntryAsync(LauncherEntryViewModel item) =>
        _list.DeleteEntryAsync(item);

    Task IEntryListViewModel.DiscoverAgentsAsync() => DiscoverAgentsAsync();
}
