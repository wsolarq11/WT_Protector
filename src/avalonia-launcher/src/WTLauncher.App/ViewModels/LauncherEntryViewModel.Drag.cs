namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel
{
    private DragCueState _dragCueState = DragCueState.None;

    public bool IsDragPreviewTarget => _dragCueState is DragCueState.TargetBefore or DragCueState.TargetAfter;

    public bool IsDragPreviewSource => _dragCueState == DragCueState.Source;

    public bool IsDragInsertBefore => _dragCueState == DragCueState.TargetBefore;

    public bool IsDragInsertAfter => _dragCueState == DragCueState.TargetAfter;

    internal void ShowDragPreviewSource() => SetDragCueState(DragCueState.Source);

    internal void ShowDragPreviewTarget(bool insertAfter) =>
        SetDragCueState(insertAfter ? DragCueState.TargetAfter : DragCueState.TargetBefore);

    internal void ClearDragCue() => SetDragCueState(DragCueState.None);

    private void SetDragCueState(DragCueState state)
    {
        if (_dragCueState == state)
            return;

        _dragCueState = state;
        OnPropertyChanged(nameof(IsDragPreviewTarget));
        OnPropertyChanged(nameof(IsDragPreviewSource));
        OnPropertyChanged(nameof(IsDragInsertBefore));
        OnPropertyChanged(nameof(IsDragInsertAfter));
    }

    private enum DragCueState
    {
        None,
        Source,
        TargetBefore,
        TargetAfter,
    }
}
