using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Keyboard reorder workflow: Move Up / Move Down in custom sort mode with
/// visible-list target resolution and one canonical order save.
/// </summary>
public sealed partial class LauncherReorderCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly LayoutManager _layout;
    private readonly ICommandHost _host;

    public LauncherReorderCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        LayoutManager layout,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public bool CanMoveEntryUp(LauncherEntryViewModel entry)
    {
        var index = _entries.MutableVisibleEntries.IndexOf(entry);
        return _layout.IsCustomSortMode && index > 0;
    }

    public bool CanMoveEntryDown(LauncherEntryViewModel entry)
    {
        var index = _entries.MutableVisibleEntries.IndexOf(entry);
        return _layout.IsCustomSortMode && index >= 0 && index < _entries.MutableVisibleEntries.Count - 1;
    }

    public void MoveEntryUp(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: -1, insertAfter: false);

    public void MoveEntryDown(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: 1, insertAfter: true);

    public void RefreshReorderCommandState()
    {
        foreach (var entry in _entries.MutableVisibleEntries)
            entry.RefreshReorderCommands();
    }

    private void MoveEntryRelative(LauncherEntryViewModel entry, int targetOffset, bool insertAfter)
    {
        _host.EnsureCustomOrderContainsEntries();

        if (!TryResolveVisibleMoveTarget(entry, targetOffset, out var target))
        {
            ApplyStatus(LauncherDragReorderStatus.MoveUnavailable);
            RefreshReorderCommandState();
            return;
        }

        var settings = _context.Store.Select(s => s.Settings);
        var (changed, updatedOrder) = LauncherEntryOrder.MoveRelative(settings.CustomEntryOrder, entry.Id, target.Id, insertAfter);
        if (!changed)
        {
            _host.EnsureCustomOrderContainsEntries();
            _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
            ApplyStatus(LauncherDragReorderStatus.MoveUnavailable);
            return;
        }

        var storeFile = _host.BuildStoreFile() with { Settings = settings with { CustomEntryOrder = updatedOrder } };
        var saveFailure = _host.SaveStoreFile(storeFile);
        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        ApplyStatus(saveFailure is null
            ? LauncherDragReorderStatus.Moved(entry.Name)
            : LauncherStatusContent.Error(saveFailure));
    }

    private bool TryResolveVisibleMoveTarget(
        LauncherEntryViewModel entry,
        int targetOffset,
        out LauncherEntryViewModel target)
    {
        var sourceIndex = _entries.MutableVisibleEntries.IndexOf(entry);
        var targetIndex = sourceIndex + targetOffset;
        if (!_layout.IsCustomSortMode || sourceIndex < 0 || targetIndex < 0 || targetIndex >= _entries.MutableVisibleEntries.Count)
        {
            target = entry;
            return false;
        }

        target = _entries.MutableVisibleEntries[targetIndex];
        return true;
    }

    private void ApplyStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            _context.Status.Apply(status);
    }
}
