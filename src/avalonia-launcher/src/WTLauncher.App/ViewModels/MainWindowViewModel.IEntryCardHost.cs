using WTLauncher.App.Features.Entries;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Explicit IEntryCardHost implementation + launch delegation to
// LaunchOrchestrator. The 4 reorder methods (MoveEntryUp/Down,
// CanMoveEntryUp/Down) are already public on MainWindowViewModel,
// so no forwarding is needed for those.
public sealed partial class MainWindowViewModel : IEntryCardHost
{
    CardLayoutProvider IEntryCardHost.CardLayout => _layoutManager.CardLayout;

    // ── internal launch methods (used by EntryManager bridge delegates) ──

    internal void ShowDryRun(LauncherEntryViewModel item) =>
        _launchOrchestrator.ShowDryRun(item);

    internal Task LaunchAsync(LauncherEntryViewModel item) =>
        _launchOrchestrator.LaunchAsync(item);

    // ── IEntryCardHost ──

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
