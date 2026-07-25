using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Drag reorder session management. Split from MainWindowViewModel.cs as part of
// 07-09-split-mainviewmodel-godclass Stage 1. All drag state lives on this partial;
// the workflow instance is the single owner (see spec/views/drag-drop.md).

public sealed partial class MainWindowViewModel
{
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    // These fields track drag-preview debounce state and are only accessed from the UI thread
    // (via PointerMoved → PreviewMoveEntry). No lock or volatile is needed.
    private (string DraggedId, string TargetId, bool InsertAfter)? _lastPreviewParams;
    private long _lastPreviewTick;

    // ponytail: View checks this in OnDragPointerMoved to detect ViewModel-side
    // cancellation (Escape, search, sort, preset, etc.) and flush View drag state.
    internal bool HasActiveDragReorder => _dragReorderWorkflow.HasActiveSession;

    public bool WouldChangePreview(string draggedEntryId, string targetEntryId, bool insertAfter) =>
        _dragReorderWorkflow.WouldChangePreview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);

    public void BeginDragReorder(string draggedEntryId)
    {
        _lastPreviewParams = null; // reset debounce on new drag
        EnsureDragSortMode();
        if (_dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId))
        {
            // CFA Step 1c: Workflow owns CustomEntryOrder during drag;
            // persisted on commit (Task 1d eliminates workflow _settings entirely).
            ApplyFilter();
        }
    }

    /// <summary>
    /// ponytail: 用户要求拖拽始终可用,不由排序方式限定。非 custom 模式下,
    /// 拖拽开始时切到 custom,这样 LauncherDragReorderWorkflow 的 isCustomSortMode=true
    /// 路径生效,customEntryOrder 立即可视化重排。Workflow 签名零改动。
    /// </summary>
    private void EnsureDragSortMode()
    {
        if (IsCustomSortMode) return;
        SelectedSortMode = _layoutManager.SortModes.First(m => m.Id == LauncherEntryQuery.CustomSortModeId);
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
        // CFA Step 1c: Workflow owns CustomEntryOrder during drag; persisted on commit.
        if (result.RefreshEntries)
        {
            // Debounce: skip redundant reorder with same drag params within 16ms.
            var now = Environment.TickCount64;
            if (_lastPreviewParams is { DraggedId: var d, TargetId: var t, InsertAfter: var a }
                && d == draggedEntryId && t == targetEntryId && a == insertAfter
                && (now - _lastPreviewTick) < 16)
            {
                return result.Accepted;
            }
            _lastPreviewParams = (draggedEntryId, targetEntryId, insertAfter);
            _lastPreviewTick = now;

            // P0: Skip full ApplyFilter() during drag preview.
            // Only reorder the 2 affected entries in _list.MutableVisibleEntries.
            // Card sizes, filter text, and visible set do not change during drag.
            if (_dragReorderWorkflow.HasActiveSession)
                ReorderVisibleEntryForDrag(draggedEntryId, targetEntryId, insertAfter);
            else
                ApplyFilter();
        }

        return result.Accepted;
    }

    /// <summary>
    /// P0: Directly reorder 2 entries in _list.MutableVisibleEntries during drag.
    /// Avoids the full FilterAndSort → SyncVisibleEntries → ApplyCardSize chain.
    /// O(n) single pass to find indices, then one Move() call.
    /// </summary>
    /// <remarks>
    /// NOTE: This method's equivalence to "FilterAndSort + modified CustomEntryOrder"
    /// is verified by 7 example-based drag equivalence tests in
    /// MainWindowViewModelTests.DragTests.cs. The tests cover single-move, multi-move,
    /// insert-before, insert-after, and edge cases with filtered entries.
    /// A property-based test (e.g. FsCheck / CsCheck) would still be ideal
    /// for exhaustive future confidence.
    /// </remarks>
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

        int newIdx;
        if (insertAfter)
            newIdx = dragIdx < targetIdx ? targetIdx : targetIdx + 1;
        else
            newIdx = dragIdx < targetIdx ? targetIdx - 1 : targetIdx;

        newIdx = Math.Max(0, Math.Min(newIdx, _list.MutableVisibleEntries.Count - 1));
        if (dragIdx == newIdx) return;

        _list.MutableVisibleEntries.Move(dragIdx, newIdx);
        // Position map removed with SyncVisibleEntries incremental rebuild.
    }

    public void ClearDragHover()
    {
        _dragReorderWorkflow.ClearHover();
    }

    /// <remarks>
    /// Exceptions from <see cref="LauncherDragReorderWorkflow.Commit"/> propagate
    /// to the UI event handler (<see cref="Views.MainWindow.OnEntryDragPointerPressed"/>
    /// / DoDragReorderAsync's catch block), which sets <c>DragDropEffects.None</c>.
    /// We intentionally do not swallow exceptions here — the caller owns the error path.
    /// </remarks>
    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        // CFA Step 1c: _settings field eliminated — SaveSettingsToStore callback
        // inside the workflow persists the committed settings (including CustomEntryOrder).
        ApplyFilter();
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }

    /// <remarks>
    /// Exceptions from <see cref="LauncherDragReorderWorkflow.Cancel"/> propagate
    /// to the UI event handler (same path as <see cref="CommitDragReorder"/>).
    /// The caller sets <c>DragDropEffects.None</c> on failure.
    /// </remarks>
    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        // CFA Step 1c: _settings field eliminated — CurrentSettings reads from store grain.
        // Workflow Cancel restores pre-drag state internally; no store mutation needed.
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }

    private void DiscardDragReorderPreview()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.RefreshEntries)
        {
            // CFA Step 1c: _settings field eliminated — CurrentSettings reads from store grain.
            // Workflow Cancel restores pre-drag state internally; no store mutation needed.
            ApplyFilter();
        }
    }
}
