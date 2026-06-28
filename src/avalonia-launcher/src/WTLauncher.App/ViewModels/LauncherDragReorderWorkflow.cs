using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDragReorderWorkflow
{
    private LauncherSettings _settings;
    private readonly Func<LauncherSettings, string?> _saveSettings;
    private readonly IReadOnlyList<LauncherEntryViewModel> _entries;
    private LauncherDragReorderSession? _session;

    public bool HasActiveSession => _session is not null;

    /// <summary>The current settings. May be replaced during restore operations —
    /// callers should sync their reference after Cancel/Discard.</summary>
    public LauncherSettings Settings => _settings;

    /// <param name="saveSettings">Callback that persists the given settings and
    /// returns null on success or an error message on failure.</param>
    public LauncherDragReorderWorkflow(
        LauncherSettings settings,
        IReadOnlyList<LauncherEntryViewModel> entries,
        Func<LauncherSettings, string?> saveSettings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(saveSettings);

        _settings = settings;
        _entries = entries;
        _saveSettings = saveSettings;
    }

    /// <summary>
    /// Begins a drag reorder operation. Returns <c>true</c> when the caller should
    /// refresh visible entries (e.g. because a stale preview state was restored)
    /// — NOT an indicator of whether a session was successfully created.
    /// Use <see cref="HasActiveSession"/> to determine whether a session exists.
    /// </summary>
    public bool Begin(bool isCustomSortMode, string draggedEntryId)
    {
        var refreshEntries = TryRestorePreviewState();

        if (!isCustomSortMode)
            return refreshEntries;

        EnsureCustomOrderContainsEntries();

        var source = FindEntry(draggedEntryId);
        if (source is null)
            return refreshEntries;

        _session = LauncherDragReorderSession.Start(
            source,
            LauncherSettingsSnapshot.Capture(_settings));
        return refreshEntries;
    }

    public bool WouldChangePreview(bool isCustomSortMode, string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        if (!isCustomSortMode)
            return false;

        if (LauncherEntryIds.SameNormalized(draggedEntryId, targetEntryId))
            return false;

        var session = _session;
        if (session is null || !session.IsFor(draggedEntryId))
            return true; // new session or different source — will change

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
        if (LauncherEntryIds.SameNormalized(draggedEntryId, targetEntryId))
            return (false, refreshEntries);

        var target = FindEntry(targetEntryId);
        if (target is null)
        {
            session.ClearHover();
            return (false, refreshEntries);
        }

        if (!session.TryRecordPreview(targetEntryId, insertAfter))
            return (false, refreshEntries);

        EnsureCustomOrderContainsEntries();
        var (changed, updatedOrder) = LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        if (!changed)
            return (false, refreshEntries);
        _settings = _settings with { CustomEntryOrder = updatedOrder };

        session.SetTarget(target, insertAfter);
        return (true, true);
    }

    private LauncherEntryViewModel? FindEntry(string entryId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (LauncherEntryIds.Same(_entries[i].Id, entryId))
                return _entries[i];
        return null;
    }

    private void EnsureCustomOrderContainsEntries()
    {
        _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, _entries.Select(entry => entry.Id)) };
    }

    public void ClearHover()
    {
        _session?.ClearHover();
    }

    public (string? Status, bool RefreshEntries) Commit(bool isCustomSortMode)
    {
        if (!isCustomSortMode)
            return (null, false);
        if (_session is null)
            return (null, false);

        var session = _session;
        try
        {
            var saveFailure = _saveSettings(_settings);
            if (saveFailure is not null)
            {
                // Rollback settings on save failure so the caller sees the pre-commit order.
                _settings = session.Snapshot.RestoreCustomEntryOrder(_settings);
                return (saveFailure, true);
            }

            return (LauncherDragReorderStatus.Saved, false);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            // Rollback settings on any unexpected commit failure.
            _settings = session.Snapshot.RestoreCustomEntryOrder(_settings);
            return ($"保存失败: {ex.Message}", true);
        }
        finally
        {
            ClearPreviewState();
        }
    }

    public (string? Status, bool RefreshEntries) Cancel()
    {
        if (!TryRestorePreviewState())
            return (null, false);

        return (LauncherDragReorderStatus.Canceled, true);
    }

    private bool TryRestorePreviewState()
    {
        var session = _session;
        if (session is null)
            return false;

        _settings = session.Snapshot.RestoreCustomEntryOrder(_settings);
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
