using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    private (string DraggedId, string TargetId, bool InsertAfter)? _lastPreviewParams;
    private long _lastPreviewTick;

    public bool WouldChangePreview(string draggedEntryId, string targetEntryId, bool insertAfter) =>
        _dragReorderWorkflow.WouldChangePreview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);

    public void BeginDragReorder(string draggedEntryId)
    {
        _lastPreviewParams = null; // reset debounce on new drag
        if (_dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId))
        {
            _settings = _dragReorderWorkflow.Settings;
            ApplyFilter();
        }
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
        _settings = _dragReorderWorkflow.Settings;
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
            // Only reorder the 2 affected entries in _visibleEntries.
            // Card sizes, filter text, and visible set do not change during drag.
            if (_dragReorderWorkflow.HasActiveSession)
                ReorderVisibleEntryForDrag(draggedEntryId, targetEntryId, insertAfter);
            else
                ApplyFilter();
        }

        return result.Accepted;
    }

    /// <summary>
    /// P0: Directly reorder 2 entries in _visibleEntries during drag.
    /// Avoids the full FilterAndSort → SyncVisibleEntries → ApplyCardSize chain.
    /// O(n) single pass to find indices, then one Move() call.
    /// </summary>
    /// <remarks>
    /// NOTE: This method's equivalence to "FilterAndSort + modified CustomEntryOrder"
    /// is NOT formally verified. The current manual testing shows correct behavior,
    /// but a property-based test (e.g. FsCheck / CsCheck) that asserts the resulting
    /// _visibleEntries matches the output of a full ApplyFilter() would be ideal
    /// for future confidence.
    /// </remarks>
    private void ReorderVisibleEntryForDrag(string draggedId, string targetId, bool insertAfter)
    {
        int dragIdx = -1, targetIdx = -1;
        for (int i = 0; i < _visibleEntries.Count; i++)
        {
            var id = _visibleEntries[i].Id;
            if (LauncherEntryIds.SameNormalized(id, draggedId)) dragIdx = i;
            if (LauncherEntryIds.SameNormalized(id, targetId)) targetIdx = i;
            if (dragIdx >= 0 && targetIdx >= 0) break;
        }
        if (dragIdx < 0 || targetIdx < 0 || dragIdx == targetIdx) return;

        int newIdx;
        if (insertAfter)
            newIdx = dragIdx < targetIdx ? targetIdx : targetIdx + 1;
        else
            newIdx = dragIdx < targetIdx ? targetIdx - 1 : targetIdx;

        newIdx = Math.Max(0, Math.Min(newIdx, _visibleEntries.Count - 1));
        if (dragIdx == newIdx) return;

        _visibleEntries.Move(dragIdx, newIdx);
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
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
    }

    /// <remarks>
    /// Exceptions from <see cref="LauncherDragReorderWorkflow.Cancel"/> propagate
    /// to the UI event handler (same path as <see cref="CommitDragReorder"/>).
    /// The caller sets <c>DragDropEffects.None</c> on failure.
    /// </remarks>
    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
    }
}

public static class LauncherDragReorderStatus
{
    public static string Saved => "已保存自定义拖拽排序";

    public static string Canceled => "已取消: 拖拽排序";

    public static string Moved(string entryName) => $"已保存自定义排序: {entryName}";

    public static string MoveUnavailable => "当前条目不能继续移动";
}
