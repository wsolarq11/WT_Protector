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

        var old = _dragCueState;
        _dragCueState = state;

        var oldWasTarget = old is DragCueState.TargetBefore or DragCueState.TargetAfter;
        var newIsTarget = state is DragCueState.TargetBefore or DragCueState.TargetAfter;
        if (oldWasTarget != newIsTarget)
            OnPropertyChanged(nameof(IsDragPreviewTarget));

        if ((old == DragCueState.Source) != (state == DragCueState.Source))
            OnPropertyChanged(nameof(IsDragPreviewSource));

        if ((old == DragCueState.TargetBefore) != (state == DragCueState.TargetBefore))
            OnPropertyChanged(nameof(IsDragInsertBefore));

        if ((old == DragCueState.TargetAfter) != (state == DragCueState.TargetAfter))
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
