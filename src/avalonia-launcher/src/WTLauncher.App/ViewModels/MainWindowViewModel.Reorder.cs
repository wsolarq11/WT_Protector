using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public bool CanMoveEntryUp(LauncherEntryViewModel entry) =>
        IsCustomSortMode && _visibleEntries.IndexOf(entry) > 0;

    public bool CanMoveEntryDown(LauncherEntryViewModel entry)
    {
        var index = _visibleEntries.IndexOf(entry);
        return IsCustomSortMode && index >= 0 && index < _visibleEntries.Count - 1;
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
            _settings = snapshot.RestoreCustomEntryOrder(_settings);
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
        var sourceIndex = _visibleEntries.IndexOf(entry);
        var targetIndex = sourceIndex + targetOffset;
        if (!IsCustomSortMode || sourceIndex < 0 || targetIndex < 0 || targetIndex >= _visibleEntries.Count)
        {
            target = entry;
            return false;
        }

        target = _visibleEntries[targetIndex];
        return true;
    }

    private void RefreshReorderCommandState()
    {
        foreach (var entry in _entries)
            entry.RefreshReorderCommands();
    }
}
