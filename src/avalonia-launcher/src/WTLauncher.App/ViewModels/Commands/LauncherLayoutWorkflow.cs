using Avalonia;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Layout command workflow: persists filter/sort/view/group/search/window state
/// through StoreGrain and delegates projection refresh to IProjectionSynchronizer.
/// </summary>
public sealed partial class LauncherLayoutWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly LayoutManager _layout;
    private readonly ICommandHost _host;

    public LauncherLayoutWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        LayoutManager layout,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _layout.FilterChanged += OnFilterChanged;
        _layout.SortChanged += OnSortChanged;
        _layout.ViewModeChanged += OnViewModeChanged;
        _layout.GroupKeyChanged += OnGroupKeyChanged;
    }

    private void OnFilterChanged()
    {
        var result = _context.Store.DispatchErrorOr(
            s => s with { Settings = s.Settings with { LastFilterText = _layout.FilterText } });
        if (result.IsError)
        {
            _layout.FilterText = _context.Store.Select(s => s.Settings.LastFilterText);
            ApplyStatus(LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description));
        }

        EnsureCustomOrderContainsEntries();
        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
    }

    private void OnSortChanged()
    {
        var sortMode = _layout.SelectedSortMode;
        sortMode = sortMode ?? throw new InvalidOperationException("Launcher sort mode selection is required.");
        EnsureCustomOrderContainsEntries();

        var result = _context.Store.DispatchErrorOr(
            s => s with { Settings = s.Settings with { SortMode = sortMode.Id } });
        if (result.IsError)
        {
            _layout.SelectedSortMode = _layout.ResolveSortMode(
                _context.Store.Select(s => s.Settings.SortMode));
            ApplyStatus(LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description));
        }
        else
        {
            ApplyStatus(LauncherSettingsStatus.SortModeSaved(sortMode.Label));
        }

        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        _host.RefreshReorderCommandState();
    }

    private void OnViewModeChanged()
    {
        var displayMode = _layout.SelectedDisplayMode;
        displayMode = displayMode ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");
        _layout.NotifyDisplayModeProjectionChanged();

        var result = _context.Store.DispatchErrorOr(
            s => s with { Settings = s.Settings with { EntryViewMode = displayMode.Id } });
        if (result.IsError)
        {
            _layout.SelectedDisplayMode = _layout.ResolveDisplayMode(
                _context.Store.Select(s => s.Settings.EntryViewMode));
            _layout.NotifyDisplayModeProjectionChanged();
            ApplyStatus(LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description));
        }
        else
        {
            ApplyStatus(LauncherSettingsStatus.DisplayModeSaved(displayMode.Label));
        }
    }

    private void OnGroupKeyChanged()
    {
        var result = _context.Store.DispatchErrorOr(
            s => s with { Settings = s.Settings with { CardsGroupKey = _layout.CardsGroupKey } });
        if (result.IsError)
        {
            var oldCardsGroupKey = _context.Store.Select(s => s.Settings.CardsGroupKey);
            _layout.SetGroupKeyDirect(oldCardsGroupKey);
            _host.RestoreVmStateFromStore();
            ApplyStatus(LauncherStatusContent.Error(result.Errors[0].Description));
            return;
        }

        _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
    }

    public void RecordSearchTerm()
    {
        var newHistory = LauncherSearchHistory.Record(_context.Store.Select(s => s.Settings.SearchHistory), _layout.FilterText);
        if (ReferenceEquals(newHistory, _context.Store.Select(s => s.Settings.SearchHistory)))
            return;

        PersistSettings(s => s with { SearchHistory = newHistory });
        _host.NotifySearchHistoryChanged();
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _layout.UpdateCardsAvailableSize(availableSize);
        _layout.CardLayout.Recalculate(
            _layout.CardsAvailableSize, _entries.VisibleEntries.Count);
    }

    public bool TrySaveWindowBounds(double width, double height, double x = 0, double y = 0, string windowState = "Normal")
    {
        _host.DiscardDragReorderPreview();
        try
        {
            _context.Store.Dispatch(state =>
            {
                var nextWindow = state.Settings.Window with
                {
                    Width = width,
                    Height = height,
                    X = x,
                    Y = y,
                    WindowState = windowState,
                };
                return state with { Settings = state.Settings with { Window = nextWindow } };
            });
            return true;
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ApplyStatus(LauncherSettingsStatus.WindowBoundsSaveFailed);
            ErrorLog.Write(ex, "TrySaveWindowBounds");
            return false;
        }
    }

    public bool PersistSettings(
        Func<LauncherSettings, LauncherSettings> mutator,
        LauncherStatusContent? successStatus = null)
    {
        var newSettings = mutator(_context.Store.Select(s => s.Settings));
        try
        {
            _context.Store.Dispatch(s => s with { Settings = newSettings });
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ApplyErrorStatus(LauncherPersistenceStatus.StoreSaveFailed(ex).Text);
            return false;
        }

        if (successStatus is not null)
            ApplyStatus(successStatus);

        return true;
    }

    public void EnsureCustomOrderContainsEntries()
    {
        var currentIds = _entries.MutableEntries.Select(entry => entry.Id).ToList();
        var currentOrder = _context.Store.Select(s => s.Settings.CustomEntryOrder);
        var synced = LauncherEntryOrder.SyncWithEntries(currentOrder, currentIds);
        if (!currentOrder.SequenceEqual(synced))
        {
            try
            {
                _context.Store.Dispatch(s => s with { Settings = s.Settings with { CustomEntryOrder = synced } });
                _context.SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "EnsureCustomOrderContainsEntries");
            }
        }
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
