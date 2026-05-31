using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherDragReorderWorkflow
{
    private readonly LauncherSettings _settings;
    private readonly string _settingsPath;
    private readonly IReadOnlyList<LauncherEntryViewModel> _entries;
    private readonly Action _ensureCustomOrderContainsEntries;
    private readonly Action _applyFilter;
    private LauncherDragReorderSession? _session;

    public LauncherDragReorderWorkflow(
        LauncherSettings settings,
        string settingsPath,
        IReadOnlyList<LauncherEntryViewModel> entries,
        Action ensureCustomOrderContainsEntries,
        Action applyFilter)
    {
        _settings = settings;
        _settingsPath = settingsPath;
        _entries = entries;
        _ensureCustomOrderContainsEntries = ensureCustomOrderContainsEntries;
        _applyFilter = applyFilter;
    }

    public void Begin(bool isCustomSortMode, string draggedEntryId)
    {
        TryRestorePreviewState();

        if (!isCustomSortMode)
            return;

        _ensureCustomOrderContainsEntries();

        var source = FindEntry(draggedEntryId);
        if (source is null)
            return;

        _session = LauncherDragReorderSession.Start(
            draggedEntryId,
            LauncherSettingsSnapshot.Capture(_settings),
            source);
    }

    public bool Preview(bool isCustomSortMode, string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        if (!isCustomSortMode)
            return false;
        var session = _session;
        if (session is null || !session.IsFor(draggedEntryId))
        {
            Begin(isCustomSortMode, draggedEntryId);
            session = _session;
        }
        if (session is null)
            return false;
        if (LauncherEntryIds.SameNormalized(draggedEntryId, targetEntryId))
            return false;

        var target = FindEntry(targetEntryId);
        if (target is null)
        {
            session.ClearHover();
            return false;
        }

        if (!session.TryRecordPreview(targetEntryId, insertAfter))
            return false;

        _ensureCustomOrderContainsEntries();
        LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        _applyFilter();

        session.SetTarget(target, insertAfter);
        return true;
    }

    private LauncherEntryViewModel? FindEntry(string entryId) =>
        _entries.FirstOrDefault(entry => LauncherEntryIds.SameNormalized(entry.Id, entryId));

    public void ClearHover()
    {
        _session?.ClearHover();
    }

    public string? Commit(bool isCustomSortMode)
    {
        if (!isCustomSortMode)
            return null;
        if (_session is null)
            return null;

        var session = _session;
        var saveFailure = LauncherSettingsMutationPersistence.Save(
            _settings,
            _settingsPath,
            session.Snapshot,
            () =>
            {
                _applyFilter();
                ClearPreviewState();
            });
        if (saveFailure is not null)
            return saveFailure;

        ClearPreviewState();
        return LauncherDragReorderStatus.Saved;
    }

    public string? Cancel()
    {
        if (!TryRestorePreviewState())
            return null;

        return LauncherDragReorderStatus.Canceled;
    }

    private bool TryRestorePreviewState()
    {
        var session = _session;
        if (session is null)
            return false;

        session.Snapshot.RestoreCustomEntryOrder(_settings);
        _applyFilter();
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
