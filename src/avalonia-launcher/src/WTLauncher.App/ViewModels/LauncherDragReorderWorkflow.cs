using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDragReorderWorkflow
{
    private readonly LauncherSettings _settings;
    private readonly string _settingsPath;
    private readonly IReadOnlyList<LauncherEntryViewModel> _entries;
    private LauncherDragReorderSession? _session;

    public bool HasActiveSession => _session is not null;

    public LauncherDragReorderWorkflow(
        LauncherSettings settings,
        string settingsPath,
        IReadOnlyList<LauncherEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        ArgumentNullException.ThrowIfNull(entries);

        _settings = settings;
        _settingsPath = settingsPath;
        _entries = entries;
    }

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
        if (!LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter))
            return (false, refreshEntries);

        session.SetTarget(target, insertAfter);
        return (true, true);
    }

    private LauncherEntryViewModel? FindEntry(string entryId) =>
        _entries.FirstOrDefault(entry => LauncherEntryIds.SameNormalized(entry.Id, entryId));

    private void EnsureCustomOrderContainsEntries() =>
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, _entries.Select(entry => entry.Id));

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
        var saveFailure = LauncherSettingsMutationPersistence.Save(
            _settings,
            _settingsPath,
            session.Snapshot);
        if (saveFailure is not null)
        {
            ClearPreviewState();
            return (saveFailure, true);
        }

        ClearPreviewState();
        return (LauncherDragReorderStatus.Saved, false);
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

        session.Snapshot.RestoreCustomEntryOrder(_settings);
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
