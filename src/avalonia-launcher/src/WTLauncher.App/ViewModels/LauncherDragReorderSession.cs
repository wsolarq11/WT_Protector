using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDragReorderSession
{
    private LauncherEntryViewModel? _target;
    private LauncherDragInsertPreview? _lastPreview;

    private LauncherDragReorderSession(
        string draggedEntryId,
        LauncherSettingsSnapshot snapshot,
        LauncherEntryViewModel? source)
    {
        DraggedEntryId = draggedEntryId;
        Snapshot = snapshot;
        Source = source;
    }

    public string DraggedEntryId { get; }

    public LauncherSettingsSnapshot Snapshot { get; }

    public LauncherEntryViewModel? Source { get; }

    public static LauncherDragReorderSession Start(
        string draggedEntryId,
        LauncherSettingsSnapshot snapshot,
        LauncherEntryViewModel? source)
    {
        var session = new LauncherDragReorderSession(draggedEntryId, snapshot, source);
        source?.ShowDragPreviewSource();
        return session;
    }

    public bool IsFor(string draggedEntryId)
    {
        return string.Equals(DraggedEntryId, draggedEntryId, StringComparison.Ordinal);
    }

    public bool TryRecordPreview(string targetEntryId, bool insertAfter)
    {
        var nextPreview = new LauncherDragInsertPreview(targetEntryId, insertAfter);
        if (_lastPreview == nextPreview)
            return false;

        _lastPreview = nextPreview;
        return true;
    }

    public void SetTarget(LauncherEntryViewModel? target, bool insertAfter)
    {
        if (ReferenceEquals(_target, target))
        {
            _target?.ShowDragPreviewTarget(insertAfter);
            return;
        }

        ClearTarget();
        _target = target;
        _target?.ShowDragPreviewTarget(insertAfter);
    }

    public void ClearHover()
    {
        ClearTarget();
        _lastPreview = null;
    }

    public void Clear()
    {
        ClearHover();
        Source?.ClearDragCue();
    }

    private void ClearTarget()
    {
        if (_target is null)
            return;

        _target.ClearDragCue();
        _target = null;
    }
}

internal readonly record struct LauncherDragInsertPreview(string TargetEntryId, bool InsertAfter);
