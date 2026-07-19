using WTLauncher.App.Features.Entries;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryListViewModel implementation — delegates to existing
// internal/private methods to keep the existing API surface unchanged.
public sealed partial class MainWindowViewModel : IEntryListViewModel
{
    Task IEntryListViewModel.AddEntryAsync() => AddEntryAsync();

    Task IEntryListViewModel.CloneEntryAsync(LauncherEntryViewModel item) =>
        CloneEntryAsync(item);

    Task IEntryListViewModel.EditEntryAsync(LauncherEntryViewModel item) =>
        EditEntryAsync(item);

    Task IEntryListViewModel.DeleteEntryAsync(LauncherEntryViewModel item) =>
        DeleteEntryAsync(item);

    Task IEntryListViewModel.DiscoverAgentsAsync() => DiscoverAgentsAsync();
}
