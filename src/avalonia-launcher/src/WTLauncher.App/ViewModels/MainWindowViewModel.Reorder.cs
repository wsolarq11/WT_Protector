using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public bool CanMoveEntryUp(LauncherEntryViewModel entry) =>
        IsCustomSortMode && VisibleEntries.IndexOf(entry) > 0;

    public bool CanMoveEntryDown(LauncherEntryViewModel entry)
    {
        var index = VisibleEntries.IndexOf(entry);
        return IsCustomSortMode && index >= 0 && index < VisibleEntries.Count - 1;
    }

    public void MoveEntryUp(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: -1, insertAfter: false);

    public void MoveEntryDown(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: 1, insertAfter: true);

    private void MoveEntryRelative(LauncherEntryViewModel entry, int targetOffset, bool insertAfter)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        if (!TryResolveVisibleMoveTarget(entry, targetOffset, out var target))
        {
            StatusText = LauncherDragReorderStatus.MoveUnavailable;
            RefreshReorderCommandState();
            return;
        }

        EnsureCustomOrderContainsEntries();
        if (!LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, entry.Id, target.Id, insertAfter))
        {
            snapshot.RestoreCustomEntryOrder(_settings);
            ApplyFilter();
            StatusText = LauncherDragReorderStatus.MoveUnavailable;
            return;
        }

        var saveFailure = SaveSettingsChange(snapshot);
        ApplyFilter();
        ApplyStatusText(saveFailure ?? LauncherDragReorderStatus.Moved(entry.Name));
    }

    private bool TryResolveVisibleMoveTarget(
        LauncherEntryViewModel entry,
        int targetOffset,
        out LauncherEntryViewModel target)
    {
        var sourceIndex = VisibleEntries.IndexOf(entry);
        var targetIndex = sourceIndex + targetOffset;
        if (!IsCustomSortMode || sourceIndex < 0 || targetIndex < 0 || targetIndex >= VisibleEntries.Count)
        {
            target = entry;
            return false;
        }

        target = VisibleEntries[targetIndex];
        return true;
    }

    private void RefreshReorderCommandState()
    {
        foreach (var entry in Entries)
            entry.RefreshReorderCommands();
    }
}
