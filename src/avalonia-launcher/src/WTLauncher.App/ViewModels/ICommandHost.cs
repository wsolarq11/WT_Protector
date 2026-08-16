using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Narrow UI-state host operations needed by command workflows. Workflows must not
/// receive MainWindowViewModel itself; this port keeps callback surface explicit.
/// </summary>
public interface ICommandHost
{
    public void DiscardDragReorderPreview();
    public void RefreshSelectionState();
    public void RefreshCardGroupProjections();
    public void NotifyPresetManagementCommands();
    public void RefreshReorderCommandState();
    public void RestoreVmStateFromStore();
    public void NotifySearchHistoryChanged();
    public bool ReplaceSelection(IReadOnlySet<string> entryIds);
    public LauncherStoreFile BuildStoreFile();
    public string? SaveStoreFile(LauncherStoreFile storeFile);
    public void EnsureCustomOrderContainsEntries();
    public void SyncTotalCount();
    public LauncherEntryViewModel CreateEntryViewModel(LaunchEntry entry);
}
