using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Selection command workflow: user and batched selection write paths, batch
/// selection save/restore, and batch launch delegation.
/// </summary>
public sealed partial class LauncherSelectionCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly LaunchOrchestrator _launch;
    private readonly ICommandHost _host;

    public LauncherSelectionCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        LaunchOrchestrator launch,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public void SelectVisible()
    {
        _host.DiscardDragReorderPreview();
        var targets = _entries.MutableVisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatus(_entries.MutableVisibleEntries.Count == 0
                ? LauncherSelectionStatus.NoVisibleEntries
                : LauncherSelectionStatus.VisibleAlreadySelected);
            return;
        }

        if (SetSelection(targets, selected: true))
            ApplyStatus(LauncherSelectionStatus.VisibleSelected(targets.Count));
    }

    public void ClearSelection()
    {
        _host.DiscardDragReorderPreview();
        var targets = GetSelectedEntries();
        if (targets.Count == 0)
        {
            ApplyStatus(LauncherSelectionStatus.NoSelectedEntriesToClear);
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatus(LauncherSelectionStatus.Cleared(targets.Count));
    }

    public void ClearVisibleSelection()
    {
        _host.DiscardDragReorderPreview();
        var targets = _entries.MutableVisibleEntries.Where(entry => entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatus(LauncherSelectionStatus.NoSelectedEntriesToClear);
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatus(LauncherSelectionStatus.Cleared(targets.Count));
    }

    public async Task LaunchSelectedAsync()
    {
        await _launch.LaunchBatchAsync(
            GetSelectedEntries(),
            "已勾选项",
            LauncherSelectionStatus.NoSelectedEntries,
            LauncherSelectionStatus.BatchLaunchCanceled,
            LauncherSelectionStatus.BatchLaunchFailed,
            LauncherSelectionStatus.BatchLaunchCompleted,
            LauncherRunSource.Selection).ConfigureAwait(true);
    }

    public void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        _host.DiscardDragReorderPreview();
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        var snapshot = new List<(LauncherEntryViewModel Entry, bool WasSelected)> { (item, previousSelected) };
        SaveSelectionChangeOrRestore(snapshot);
    }

    public bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        _host.DiscardDragReorderPreview();
        var snapshot = _entries.MutableEntries.Select(e => (e, e.IsSelected)).ToList();
        try
        {
            foreach (var entry in _entries.MutableEntries)
                entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id));

            return SaveSelectionChangeOrRestore(snapshot);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            foreach (var (entry, wasSelected) in snapshot)
                entry.SetSelectionFromOwner(wasSelected);
            throw;
        }
    }

    public bool CanOperateOnSelection() => _entries.MutableEntries.Any(entry => entry.IsSelected);

    public bool CanSelectVisible() => _entries.MutableVisibleEntries.Any(entry => !entry.IsSelected);

    public IReadOnlyList<LauncherEntryViewModel> GetSelectedEntries() =>
        _entries.MutableEntries.Where(entry => entry.IsSelected).ToList();

    public IReadOnlyList<string> GetSelectedEntryIds()
    {
        var ids = new List<string>(_entries.MutableEntries.Count);
        foreach (var entry in _entries.MutableEntries)
            if (entry.IsSelected) ids.Add(entry.Id);
        return ids;
    }

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        _host.DiscardDragReorderPreview();
        var snapshot = entries.Select(e => (e, e.IsSelected)).ToList();
        try
        {
            foreach (var entry in entries)
                entry.SetSelectionFromOwner(selected);

            return SaveSelectionChangeOrRestore(snapshot);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            foreach (var (entry, wasSelected) in snapshot)
                entry.SetSelectionFromOwner(wasSelected);
            throw;
        }
    }

    private bool SaveSelectionChangeOrRestore(
        List<(LauncherEntryViewModel Entry, bool WasSelected)> snapshot)
    {
        _host.RefreshSelectionState();
        using var transaction = new StoreMutationSession(
            _host.SaveStoreFile,
            () =>
            {
                foreach (var (entry, wasSelected) in snapshot)
                    entry.SetSelectionFromOwner(wasSelected);
                _host.RefreshSelectionState();
            });

        var saveFailure = transaction.Commit(_host.BuildStoreFile());
        if (saveFailure is not null)
        {
            ApplyErrorStatus(saveFailure);
            return false;
        }

        return true;
    }

    private void ApplyStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            _context.Status.Apply(status);
    }

    private void ApplyErrorStatus(string? statusText)
    {
        if (statusText is not null)
            ApplyStatus(LauncherStatusContent.Error(statusText));
    }
}
