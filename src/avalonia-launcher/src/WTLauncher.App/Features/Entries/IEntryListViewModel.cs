using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Features.Entries;

/// <summary>Entry list state and commands — the public contract of the
/// Entries feature module. The shell ViewModel holds a reference to this
/// interface instead of owning the entry list directly.</summary>
public interface IEntryListViewModel
{
    /// <summary>All entries (read-only for external consumers).</summary>
    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    /// <summary>Filtered/visible entries.</summary>
    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    /// <summary>Total entry count (before filter).</summary>
    public int TotalCount { get; }

    /// <summary>Visible entry count (after filter).</summary>
    public int VisibleCount { get; }

    /// <summary>True when no entries match the current filter.</summary>
    public bool IsVisibleEmpty { get; }

    // Entry CRUD
    public Task AddEntryAsync();
    public Task CloneEntryAsync(LauncherEntryViewModel item);
    public Task EditEntryAsync(LauncherEntryViewModel item);
    public Task DeleteEntryAsync(LauncherEntryViewModel item);

    // Discovery
    public Task DiscoverAgentsAsync();
}
