using WTLauncher.App.Features;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Keyboard-accessible Move Up / Move Down commands (non-drag alternative).
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-godclass Stage 1.
// See spec/views/drag-drop.md "Keyboard Reorder".

public sealed partial class MainWindowViewModel
{
    public bool CanMoveEntryUp(LauncherEntryViewModel entry)
    {
        var index = _list.MutableVisibleEntries.IndexOf(entry);
        return IsCustomSortMode && index > 0;
    }

    public bool CanMoveEntryDown(LauncherEntryViewModel entry)
    {
        var index = _list.MutableVisibleEntries.IndexOf(entry);
        return IsCustomSortMode && index >= 0 && index < _list.MutableVisibleEntries.Count - 1;
    }

    public void MoveEntryUp(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: -1, insertAfter: false);

    public void MoveEntryDown(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: 1, insertAfter: true);

    private void MoveEntryRelative(LauncherEntryViewModel entry, int targetOffset, bool insertAfter)
    {
        // Sync entries first so the custom order includes newly-added IDs.
        EnsureCustomOrderContainsEntries();

        if (!TryResolveVisibleMoveTarget(entry, targetOffset, out var target))
        {
            StatusContent = LauncherDragReorderStatus.MoveUnavailable;
            RefreshReorderCommandState();
            return;
        }
        var (changed, updatedOrder) = LauncherEntryOrder.MoveRelative(CurrentSettings.CustomEntryOrder, entry.Id, target.Id, insertAfter);
        if (!changed)
        {
            // Re-sync rather than restoring the snapshot: EnsureCustomOrderContainsEntries
            // may have added new entry IDs after the snapshot was captured, and restoring
            // the snapshot would lose them.
            EnsureCustomOrderContainsEntries();
            ApplyFilter();
            StatusContent = LauncherDragReorderStatus.MoveUnavailable;
            return;
        }

        var storeFile = BuildStoreFile() with { Settings = CurrentSettings with { CustomEntryOrder = updatedOrder } };
        var saveFailure = SaveStoreFile(storeFile);
        ApplyFilter();
        ApplyStatusText(
            saveFailure is null
                ? LauncherDragReorderStatus.Moved(entry.Name)
                : LauncherStatusContent.Error(saveFailure));
    }

    private bool TryResolveVisibleMoveTarget(
        LauncherEntryViewModel entry,
        int targetOffset,
        out LauncherEntryViewModel target)
    {
        var sourceIndex = _list.MutableVisibleEntries.IndexOf(entry);
        var targetIndex = sourceIndex + targetOffset;
        if (!IsCustomSortMode || sourceIndex < 0 || targetIndex < 0 || targetIndex >= _list.MutableVisibleEntries.Count)
        {
            target = entry;
            return false;
        }

        target = _list.MutableVisibleEntries[targetIndex];
        return true;
    }

    private void RefreshReorderCommandState()
    {
        foreach (var entry in _list.MutableVisibleEntries)
            entry.RefreshReorderCommands();
    }
}
