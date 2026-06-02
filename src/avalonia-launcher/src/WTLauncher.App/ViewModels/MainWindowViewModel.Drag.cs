namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    public void BeginDragReorder(string draggedEntryId)
    {
        if (_dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId))
            ApplyFilter();
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
        if (result.RefreshEntries)
            ApplyFilter();

        return result.Accepted;
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
