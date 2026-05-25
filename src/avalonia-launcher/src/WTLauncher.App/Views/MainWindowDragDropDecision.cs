using Avalonia.Input;

namespace WTLauncher.App.Views;

public static class MainWindowDragDropDecision
{
    public static MainWindowDragOverDecision EvaluateDragOver(
        bool isCustomSortMode,
        bool hasResolvedTarget,
        IDataTransfer dataTransfer,
        string? activeDragEntryId)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        var canDrop = isCustomSortMode
            && hasResolvedTarget
            && MainWindowDragTextTransfer.CanAcceptEntryId(dataTransfer);
        var previewDraggedEntryId = canDrop ? activeDragEntryId : null;

        return new MainWindowDragOverDecision(
            canDrop ? DragDropEffects.Move : DragDropEffects.None,
            canDrop,
            previewDraggedEntryId is null,
            previewDraggedEntryId);
    }

    public static MainWindowDropDecision EvaluateDrop(bool hasResolvedTarget, IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        var draggedEntryId = hasResolvedTarget
            ? MainWindowDragTextTransfer.TryGetEntryId(dataTransfer)
            : null;

        return new MainWindowDropDecision(
            draggedEntryId is null ? DragDropEffects.None : DragDropEffects.Move,
            draggedEntryId is not null,
            draggedEntryId);
    }
}

public readonly record struct MainWindowDragOverDecision(
    DragDropEffects Effects,
    bool Handled,
    bool ShouldClearHover,
    string? PreviewDraggedEntryId);

public readonly record struct MainWindowDropDecision(
    DragDropEffects Effects,
    bool Handled,
    string? DraggedEntryId);
