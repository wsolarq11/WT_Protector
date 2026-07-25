using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDragReorderSession
{
    private LauncherEntryViewModel? _target;
    private LauncherDragInsertPreview? _lastPreview;
    private readonly LauncherEntryViewModel _source;

    private LauncherDragReorderSession(
        string draggedEntryId,
        ImmutableList<string> snapshot,
        LauncherEntryViewModel source)
    {
        DraggedEntryId = draggedEntryId;
        Snapshot = snapshot;
        _source = source;
    }

    public string DraggedEntryId { get; }

    internal ImmutableList<string> Snapshot { get; }

    public static LauncherDragReorderSession Start(
        LauncherEntryViewModel source,
        ImmutableList<string> snapshot)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(snapshot);

        var session = new LauncherDragReorderSession(RequireSourceEntryId(source), snapshot, source);
        source.ShowDragPreviewSource();
        return session;
    }

    private static string RequireSourceEntryId(LauncherEntryViewModel source)
    {
        var entryId = EntryId.Normalize(source.Id);
        if (!EntryId.IsUsable(entryId))
            throw new InvalidOperationException("Drag source entry id is required.");

        return entryId;
    }

    public bool IsFor(string draggedEntryId)
    {
        return EntryId.SameNormalized(DraggedEntryId, draggedEntryId);
    }

    public bool TryRecordPreview(string targetEntryId, bool insertAfter)
    {
        var nextPreview = new LauncherDragInsertPreview(EntryId.Normalize(targetEntryId), insertAfter);
        if (_lastPreview == nextPreview)
            return false;

        _lastPreview = nextPreview;
        return true;
    }

    public bool WouldSkip(string targetEntryId, bool insertAfter)
    {
        var nextPreview = new LauncherDragInsertPreview(EntryId.Normalize(targetEntryId), insertAfter);
        return _lastPreview == nextPreview;
    }

    public void SetTarget(LauncherEntryViewModel target, bool insertAfter)
    {
        if (ReferenceEquals(_target, target))
        {
            _target.ShowDragPreviewTarget(insertAfter);
            return;
        }

        ClearTarget();
        _target = target;
        _target.ShowDragPreviewTarget(insertAfter);
    }

    public void ClearHover()
    {
        ClearTarget();
        _lastPreview = null;
    }

    public void Clear()
    {
        ClearHover();
        _source.ClearDragCue();
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
