using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>Single owner of the entry collection (both full and visible
/// projections), CRUD operations, and UI bridge delegates.
/// Uses <see cref="EntryProjection"/> for the visible projection.
///
/// Migration note: MutableEntries/MutableVisibleEntries remain as internal
/// backward-compat for Drag/Layout partials (Phase 3 migrates them).</summary>
#pragma warning disable CA1001 // EntryProjection lifecycle matches this VM
public sealed partial class EntryListViewModel : ObservableObject, IEntryListViewModel, IEntryCardHost
#pragma warning restore CA1001
{
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];
    private readonly EntryProjection _projection;
    private readonly IEntryService _entryService;
    private readonly StoreGrain _storeGrain;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly CardLayoutProvider _cardLayout;

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    /// <summary>Visible entries. Phase 2: backed by DynamicData projection.</summary>
    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    /// <summary>Mutable access for MWVM partial files (drag reorder, sync, etc.).</summary>
    internal ObservableCollection<LauncherEntryViewModel> MutableEntries => _entries;

    /// <summary>Backward-compat visible collection for Drag/Layout systems.</summary>
    internal ObservableCollection<LauncherEntryViewModel> MutableVisibleEntries => _visibleEntries;

    internal string? KeyboardFocusedEntryId { get; set; }

    [ObservableProperty]
    private int _totalCount;

    CardLayoutProvider IEntryCardHost.CardLayout => _cardLayout;

    public int VisibleCount => _projection.VisibleEntries.Count;
    public bool IsVisibleEmpty => VisibleCount == 0;

    // ── events (kept for MWVM backward compat; Phase 3 removes these) ──
    public event Action? EntriesChanged;
    public event Action? CountChanged;

    // ── UI bridge delegates (set by MWVM orchestrator) ──
    internal Action<LauncherStatusContent> StatusApplier = null!;
    internal Action DragPreviewDiscarder = null!;
    internal Action<LauncherEntryViewModel, bool> SelectionUpdater = null!;
    internal Func<bool> DetailedListModeChecker = null!;
    internal Func<bool> NamesModeChecker = null!;
    internal Action<LauncherEntryViewModel> DryRunInitiator = null!;
    internal Func<LauncherEntryViewModel, Task> AsyncLauncher = null!;
    internal Action<LauncherEntryViewModel> EntryMoverUp = null!;
    internal Func<LauncherEntryViewModel, bool> EntryMoverUpChecker = null!;
    internal Action<LauncherEntryViewModel> EntryMoverDown = null!;
    internal Func<LauncherEntryViewModel, bool> EntryMoverDownChecker = null!;

    public EntryListViewModel(
        IEntryService entryService,
        Func<DateTimeOffset> utcNow,
        StoreGrain storeGrain,
        CardLayoutProvider cardLayout)
    {
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _storeGrain = storeGrain ?? throw new ArgumentNullException(nameof(storeGrain));
        _cardLayout = cardLayout ?? throw new ArgumentNullException(nameof(cardLayout));
        _projection = new EntryProjection();
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
    }

    /// <summary>Replace all entries, creating ViewModels that own this
    /// EntryListViewModel as their <see cref="IEntryCardHost"/>.
    /// Previous ViewModels are disposed to release event subscriptions.</summary>
    public void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        // Dispose old VMs before clearing to release CardLayoutProvider subscriptions.
        foreach (var vm in _entries)
            vm.Dispose();
        _entries.Clear();
        _visibleEntries.Clear();
        foreach (var entry in entries)
        {
            var vm = new LauncherEntryViewModel(entry, this);
            _entries.Add(vm);
            _visibleEntries.Add(vm);
        }
        TotalCount = _entries.Count;
        _projection.LoadEntries(entries, this);
        EntriesChanged?.Invoke();
    }

    internal void NotifyCountChanged() => CountChanged?.Invoke();

    /// <summary>Sync the projection after entries change (add/edit/delete/clone).</summary>
    internal void SyncProjection()
    {
        _projection.LoadEntries(_entries.Select(e => e.SnapshotEntry()), this);
    }

    /// <summary>Set search filter — propagates automatically via DynamicData.</summary>
    internal void SetSearchFilter(string text) => _projection.SetFilter(text);

    /// <summary>Set sort mode — propagates automatically via DynamicData.</summary>
    internal void SetSortMode(string mode) => _projection.SetSortMode(mode);

    // ── public CRUD (IEntryListViewModel) ──

    public async Task AddEntryAsync()
    {
        DragPreviewDiscarder();
        var result = await _entryService.AddEntryAsync(
            _entries.Select(ToLaunchEntry),
            _storeGrain.Select(s => s.Settings),
            _storeGrain.Select(s => s.Settings.CommandTemplates),
            presetReferenceCount: 0,
            _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                HandleAddOrCloneResult(success.Value, static e => LauncherEntryStatus.Added(e.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        DragPreviewDiscarder();
        var result = await _entryService.CloneEntryAsync(
            item, _entries.Select(e => e.Id), _entries.Select(ToLaunchEntry),
            _storeGrain.Select(s => s.Settings),
            _storeGrain.Select(s => s.Settings.CommandTemplates),
            _storeGrain.Select(s => s.Presets.Count(p => p.EntryIds.Any(eid => EntryId.Same(eid, item.Id)))),
            _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                HandleAddOrCloneResult(success.Value, static e =>
                    LauncherEntryStatus.Added(e.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DragPreviewDiscarder();
        var result = await _entryService.EditEntryAsync(
            item, _entries.Select(ToLaunchEntry),
            _storeGrain.Select(s => new LauncherPresetFile { Presets = s.Presets, DefaultPresetId = s.DefaultPresetId }),
            _storeGrain.Select(s => s.Settings),
            _storeGrain.Select(s => s.Settings.CommandTemplates),
            _storeGrain.Select(s => s.Presets.Count(p => p.EntryIds.Any(eid => EntryId.Same(eid, item.Id)))),
            _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                await HandleEditResult(item, success.Value);
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DragPreviewDiscarder();
        var result = await _entryService.DeleteEntryAsync(
            item, KeyboardFocusedEntryId).ConfigureAwait(true);
        EntryDeleteApproval approval;
        switch (result)
        {
            case ServiceSuccess<EntryDeleteApproval> success:
                approval = success.Value;
                break;
            case ServiceCanceled<EntryDeleteApproval> canceled:
                StatusApplier(canceled.Status);
                return;
            case ServiceFailure<EntryDeleteApproval> failure:
                StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }

        if (approval.ClearKeyboardFocusId is not null)
            KeyboardFocusedEntryId = null;

        var target = approval.Target;
        var settings = _storeGrain.Select(s => s.Settings);
        var presetFile = _storeGrain.Select(s => new LauncherPresetFile { Presets = s.Presets, DefaultPresetId = s.DefaultPresetId });

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(
            presetFile, settings.CustomEntryOrder, target.EntryId);
        var newSettings = settings with { CustomEntryOrder = updatedOrder };

        try
        {
            _storeGrain.Dispatch(state => state with
            {
                Entries = state.Entries.RemoveAll(e => EntryId.Same(e.Id, target.EntryId)),
                Settings = newSettings,
                Presets = updatedPresetFile.Presets,
                DefaultPresetId = updatedPresetFile.DefaultPresetId,
            });
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeleteEntry.Mutate");
            StatusApplier(LauncherStatusContent.Error(
                LauncherPersistenceStatus.StoreSaveFailed(ex).Text));
            return;
        }

        item.Dispose(); // Release event subscriptions (CardLayoutProvider, etc.)
        _entries.Remove(item);
        TotalCount = _entries.Count;
        StatusApplier(LauncherEntryStatus.Deleted(target.EntryName));
        SyncProjection();
        EntriesChanged?.Invoke();
    }

    public Task DiscoverAgentsAsync()
    {
        return Task.CompletedTask;
    }

    // ── internal helpers ──

    private void HandleAddOrCloneResult(LaunchEntry entry, Func<LaunchEntry, LauncherStatusContent> statusFactory)
    {
        var settings = _storeGrain.Select(s => s.Settings);
        var newSettings = settings with
        {
            CustomEntryOrder = settings.CustomEntryOrder.Add(entry.Id),
        };

        try
        {
            _storeGrain.Dispatch(state => state with
            {
                Entries = state.Entries.Add(entry),
                Settings = newSettings,
            });
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddOrClone.Mutate");
            StatusApplier(LauncherStatusContent.Error(
                LauncherPersistenceStatus.StoreSaveFailed(ex).Text));
            return;
        }

        var vm = new LauncherEntryViewModel(entry, this);
        _entries.Add(vm);
        TotalCount = _entries.Count;
        StatusApplier(statusFactory(entry));
        SyncProjection();
        EntriesChanged?.Invoke();
    }

    private async Task HandleEditResult(LauncherEntryViewModel item, LaunchEntry updated)
    {
        var oldId = item.Id;
        var settings = _storeGrain.Select(s => s.Settings);
        var presetFile = _storeGrain.Select(s => new LauncherPresetFile { Presets = s.Presets, DefaultPresetId = s.DefaultPresetId });

        if (!EntryId.Same(oldId, updated.Id))
        {
            try
            {
                var (updatedPresetFile, updatedOrder, _) = LauncherEntryReferences.RenameEntryId(
                    presetFile, settings.CustomEntryOrder, oldId, updated.Id);
                presetFile = updatedPresetFile;
                settings = settings with { CustomEntryOrder = updatedOrder };
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "EditEntry.RenameReference");
                StatusApplier(LauncherEntryStatus.EditDialogFailed(item.Name, ex));
                return;
            }
        }

        updated = updated with { UpdatedAtUtc = CurrentTimestampUtc() };

        try
        {
            _storeGrain.Dispatch(state =>
            {
                var updatedEntries = state.Entries
                    .Select(e => EntryId.Same(e.Id, oldId) ? updated : e)
                    .ToImmutableList();
                return state with
                {
                    Entries = updatedEntries,
                    Settings = settings,
                    Presets = presetFile.Presets,
                    DefaultPresetId = presetFile.DefaultPresetId,
                };
            });
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "EditEntry.Mutate");
            StatusApplier(LauncherStatusContent.Error(
                LauncherPersistenceStatus.StoreSaveFailed(ex).Text));
            return;
        }

        item.ReplaceEntryFromOwner(updated, updated.UpdatedAtUtc);
        StatusApplier(LauncherEntryStatus.Updated(item.Name));
        SyncProjection();
        EntriesChanged?.Invoke();
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");
    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel e) => e.SnapshotEntry();

    // ── IEntryCardHost ──

    bool IEntryCardHost.IsDetailedListMode => DetailedListModeChecker();
    bool IEntryCardHost.IsNamesViewMode => NamesModeChecker();
    void IEntryCardHost.UpdateSelection(LauncherEntryViewModel entry, bool selected) =>
        SelectionUpdater(entry, selected);
    void IEntryCardHost.ShowDryRun(LauncherEntryViewModel entry) =>
        DryRunInitiator(entry);
    Task IEntryCardHost.LaunchAsync(LauncherEntryViewModel entry) =>
        AsyncLauncher(entry);
    Task IEntryCardHost.EditEntryAsync(LauncherEntryViewModel entry) =>
        EditEntryAsync(entry);
    Task IEntryCardHost.DeleteEntryAsync(LauncherEntryViewModel entry) =>
        DeleteEntryAsync(entry);
    Task IEntryCardHost.CloneEntryAsync(LauncherEntryViewModel entry) =>
        CloneEntryAsync(entry);
    void IEntryCardHost.MoveEntryUp(LauncherEntryViewModel entry) =>
        EntryMoverUp(entry);
    bool IEntryCardHost.CanMoveEntryUp(LauncherEntryViewModel entry) =>
        EntryMoverUpChecker(entry);
    void IEntryCardHost.MoveEntryDown(LauncherEntryViewModel entry) =>
        EntryMoverDown(entry);
    bool IEntryCardHost.CanMoveEntryDown(LauncherEntryViewModel entry) =>
        EntryMoverDownChecker(entry);
}
