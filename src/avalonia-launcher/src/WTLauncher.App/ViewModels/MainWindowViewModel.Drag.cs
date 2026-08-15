using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    // UI-thread-only drag-preview debounce state.
    private (string DraggedId, string TargetId, bool InsertAfter)? _lastPreviewParams;
    private long _lastPreviewTick;

    internal bool HasActiveDragReorder => _dragReorderWorkflow.HasActiveSession;

    public bool WouldChangePreview(string draggedEntryId, string targetEntryId, bool insertAfter) =>
        _dragReorderWorkflow.WouldChangePreview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);

    public void BeginDragReorder(string draggedEntryId)
    {
        _lastPreviewParams = null;
        EnsureDragSortMode();
        if (_dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId))
            SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
    }

    // Non-custom modes start drags by switching to custom sort so reorder stays available.
    private void EnsureDragSortMode()
    {
        if (IsCustomSortMode) return;
        SelectedSortMode = _layoutManager.SortModes.First(m => m.Id == LauncherEntryQuery.CustomSortModeId);
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
        if (!result.RefreshEntries)
            return result.Accepted;

        var now = Environment.TickCount64;
        if (_lastPreviewParams is { DraggedId: var d, TargetId: var t, InsertAfter: var a }
            && d == draggedEntryId && t == targetEntryId && a == insertAfter
            && (now - _lastPreviewTick) < 16)
            return result.Accepted;

        _lastPreviewParams = (draggedEntryId, targetEntryId, insertAfter);
        _lastPreviewTick = now;
        if (_dragReorderWorkflow.HasActiveSession)
            ReorderVisibleEntryForDrag(draggedEntryId, targetEntryId, insertAfter);
        else
            SynchronizeProjections(ProjectionSyncMode.VisibleOnly);

        return result.Accepted;
    }

    // During drag, reorder only the two affected visible entries instead of
    // running the full filter/sort pipeline.
    private void ReorderVisibleEntryForDrag(string draggedId, string targetId, bool insertAfter)
    {
        int dragIdx = -1, targetIdx = -1;
        for (int i = 0; i < _list.MutableVisibleEntries.Count; i++)
        {
            var id = _list.MutableVisibleEntries[i].Id;
            if (EntryId.Same(id, draggedId)) dragIdx = i;
            if (EntryId.Same(id, targetId)) targetIdx = i;
            if (dragIdx >= 0 && targetIdx >= 0) break;
        }
        if (dragIdx < 0 || targetIdx < 0 || dragIdx == targetIdx) return;

        var newIdx = insertAfter
            ? dragIdx < targetIdx ? targetIdx : targetIdx + 1
            : dragIdx < targetIdx ? targetIdx - 1 : targetIdx;
        newIdx = Math.Max(0, Math.Min(newIdx, _list.MutableVisibleEntries.Count - 1));
        if (dragIdx != newIdx)
            _list.MutableVisibleEntries.Move(dragIdx, newIdx);
    }

    public void ClearDragHover() => _dragReorderWorkflow.ClearHover();

    // Workflow commit/cancel errors propagate to the View drag handler, which
    // owns the DragDropEffects error path.
    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.RefreshEntries)
            SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }

    private void DiscardDragReorderPreview()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.RefreshEntries)
            SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
    }
}
