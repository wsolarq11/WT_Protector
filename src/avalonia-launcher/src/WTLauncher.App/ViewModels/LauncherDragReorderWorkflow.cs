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
        if (!isCustomSortMode)
            return;

        _ensureCustomOrderContainsEntries();
        ClearPreviewState();

        var source = _entries.FirstOrDefault(entry => string.Equals(entry.Id, draggedEntryId, StringComparison.Ordinal));
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
        if (string.Equals(draggedEntryId, targetEntryId, StringComparison.Ordinal))
            return false;

        if (!session.TryRecordPreview(targetEntryId, insertAfter))
            return false;

        _ensureCustomOrderContainsEntries();
        LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        _applyFilter();

        var target = _entries.FirstOrDefault(entry => string.Equals(entry.Id, targetEntryId, StringComparison.Ordinal));
        session.SetTarget(target, insertAfter);
        return true;
    }

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
        return "已保存自定义拖拽排序";
    }

    public string? Cancel()
    {
        var session = _session;
        if (session is null)
            return null;

        session.Snapshot.RestoreCustomEntryOrder(_settings);
        _applyFilter();
        ClearPreviewState();
        return "已取消: 拖拽排序";
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
