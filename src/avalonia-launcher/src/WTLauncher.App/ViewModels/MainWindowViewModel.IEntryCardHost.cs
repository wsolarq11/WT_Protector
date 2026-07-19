using WTLauncher.App.Features.Entries;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryCardHost implementation — delegates to existing internal
// methods to keep method accessibility unchanged. The 4 reorder methods
// (MoveEntryUp/Down, CanMoveEntryUp/Down) are already public on
// MainWindowViewModel, so no forwarding is needed for those.
public sealed partial class MainWindowViewModel : IEntryCardHost
{
    void IEntryCardHost.UpdateSelection(LauncherEntryViewModel item, bool selected) =>
        UpdateSelection(item, selected);

    void IEntryCardHost.ShowDryRun(LauncherEntryViewModel item) =>
        ShowDryRun(item);

    Task IEntryCardHost.LaunchAsync(LauncherEntryViewModel item) =>
        LaunchAsync(item);

    Task IEntryCardHost.EditEntryAsync(LauncherEntryViewModel item) =>
        EditEntryAsync(item);

    Task IEntryCardHost.DeleteEntryAsync(LauncherEntryViewModel item) =>
        DeleteEntryAsync(item);

    Task IEntryCardHost.CloneEntryAsync(LauncherEntryViewModel item) =>
        CloneEntryAsync(item);
}
