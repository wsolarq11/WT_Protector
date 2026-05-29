namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    public void BeginDragReorder(string draggedEntryId)
    {
        _dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId);
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        return _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
    }

    public void ClearDragHover()
    {
        _dragReorderWorkflow.ClearHover();
    }

    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        ApplyStatusText(result);
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        ApplyStatusText(result);
    }
}
