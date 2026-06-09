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
            ApplyFilter();
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
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

    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
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
