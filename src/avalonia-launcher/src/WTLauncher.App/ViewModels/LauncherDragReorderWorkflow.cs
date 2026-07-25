using System.Collections.Immutable;
using Stateless;
using WTLauncher.App.Features;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public enum DragState
{
    Idle,
    Dragging,
    Committing
}

public enum DragTrigger
{
    Begin,
    Preview,
    Commit,
    Cancel,
    Done
}

/// <summary>
/// Drag-reorder state machine with working-copy CustomEntryOrder.
/// CFA Step 1d: no longer holds LauncherSettings — only the order list.
/// Commits order via save callback; Cancel restores from session snapshot.
/// </summary>
public sealed class LauncherDragReorderWorkflow
{
    private readonly StateMachine<DragState, DragTrigger> _machine;

    private ImmutableList<string> _customEntryOrder;
    private readonly Func<ImmutableList<string>, string?> _saveOrder;
    private readonly IReadOnlyList<LauncherEntryViewModel> _entries;
    private LauncherDragReorderSession? _session;

    public bool HasActiveSession => _session is not null;

    /// <summary>Current working CustomEntryOrder (for test assertions).</summary>
    internal System.Collections.Immutable.ImmutableList<string> CurrentOrder => _customEntryOrder;

    /// <param name="saveOrder">Callback that persists the given CustomEntryOrder and
    /// returns null on success or an error message on failure.</param>
    public LauncherDragReorderWorkflow(
        ImmutableList<string> customEntryOrder,
        IReadOnlyList<LauncherEntryViewModel> entries,
        Func<ImmutableList<string>, string?> saveOrder)
    {
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(saveOrder);

        _customEntryOrder = customEntryOrder;
        _entries = entries;
        _saveOrder = saveOrder;

        _machine = new StateMachine<DragState, DragTrigger>(DragState.Idle);

        _machine.Configure(DragState.Idle)
            .Permit(DragTrigger.Begin, DragState.Dragging);

        _machine.Configure(DragState.Dragging)
            .PermitReentry(DragTrigger.Preview)
            .Permit(DragTrigger.Commit, DragState.Committing)
            .Permit(DragTrigger.Cancel, DragState.Idle);

        _machine.Configure(DragState.Committing)
            .Permit(DragTrigger.Done, DragState.Idle);
    }

    /// <summary>
    /// Begins a drag reorder operation. Returns <c>true</c> when the caller should
    /// refresh visible entries — NOT an indicator of whether a session was created.
    /// </summary>
    public bool Begin(bool isCustomSortMode, string draggedEntryId)
    {
        var refreshEntries = TryRestorePreviewState();

        if (!isCustomSortMode)
            return refreshEntries;

        SyncCustomOrder();

        var source = FindEntry(draggedEntryId);
        if (source is null)
            return refreshEntries;

        _session = LauncherDragReorderSession.Start(source, _customEntryOrder);

        if (_machine.State == DragState.Idle)
            _machine.Fire(DragTrigger.Begin);

        return refreshEntries;
    }

    public bool WouldChangePreview(bool isCustomSortMode, string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        if (!isCustomSortMode)
            return false;

        if (EntryId.SameNormalized(draggedEntryId, targetEntryId))
            return false;

        var session = _session;
        if (session is null || !session.IsFor(draggedEntryId))
            return true;

        return !session.WouldSkip(targetEntryId, insertAfter);
    }

    public (bool Accepted, bool RefreshEntries) Preview(bool isCustomSortMode, string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        if (!isCustomSortMode)
            return (false, false);

        var refreshEntries = false;
        var session = _session;
        if (session is null || !session.IsFor(draggedEntryId))
        {
            refreshEntries = Begin(isCustomSortMode, draggedEntryId);
            session = _session;
        }
        if (session is null)
            return (false, refreshEntries);

        if (_machine.State == DragState.Dragging)
            _machine.Fire(DragTrigger.Preview);

        if (EntryId.SameNormalized(draggedEntryId, targetEntryId))
            return (false, refreshEntries);

        var target = FindEntry(targetEntryId);
        if (target is null)
        {
            session.ClearHover();
            return (false, refreshEntries);
        }

        if (!session.TryRecordPreview(targetEntryId, insertAfter))
            return (false, refreshEntries);

        SyncCustomOrder();
        var (changed, updatedOrder) = LauncherEntryOrder.MoveRelative(_customEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        if (!changed)
            return (false, refreshEntries);
        _customEntryOrder = updatedOrder;

        session.SetTarget(target, insertAfter);
        return (true, true);
    }

    private LauncherEntryViewModel? FindEntry(string entryId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (EntryId.Same(_entries[i].Id, entryId))
                return _entries[i];
        return null;
    }

    private void SyncCustomOrder()
    {
        _customEntryOrder = LauncherEntryOrder.SyncWithEntries(_customEntryOrder, _entries.Select(entry => entry.Id));
    }

    public void ClearHover()
    {
        _session?.ClearHover();
    }

    public (LauncherStatusContent? Status, bool RefreshEntries) Commit(bool isCustomSortMode)
    {
        if (_machine.State != DragState.Dragging)
            return (null, false);

        if (!isCustomSortMode)
            return (null, false);

        var session = _session;
        if (session is null)
            return (null, false);

        _machine.Fire(DragTrigger.Commit);

        try
        {
            var saveFailure = _saveOrder(_customEntryOrder);
            if (saveFailure is not null)
            {
                _customEntryOrder = session.Snapshot;
                return (LauncherStatusContent.Error(saveFailure), true);
            }

            return (LauncherDragReorderStatus.Saved, false);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            _customEntryOrder = session.Snapshot;
            return (LauncherRefreshStatus.SaveFailed(ex), true);
        }
        finally
        {
            ClearPreviewState();
            _machine.Fire(DragTrigger.Done);
        }
    }

    public (LauncherStatusContent? Status, bool RefreshEntries) Cancel()
    {
        if (_machine.State != DragState.Dragging)
            return (null, false);

        _machine.Fire(DragTrigger.Cancel);

        if (!TryRestorePreviewState())
            return (null, false);

        return (LauncherDragReorderStatus.Canceled, true);
    }

    private bool TryRestorePreviewState()
    {
        var session = _session;
        if (session is null)
            return false;

        _customEntryOrder = session.Snapshot;
        ClearPreviewState();
        return true;
    }

    private void ClearPreviewState()
    {
        var session = _session;
        if (session is null)
            return;

        _session = null;
        session.Clear();
    }
}
