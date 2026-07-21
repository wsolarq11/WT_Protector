using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using WTLauncher.Core.Messaging;

namespace WTLauncher.App.Features.Entries;

/// <summary>Owns the entry collection and CRUD operations. Shares the
/// ObservableCollection references with the shell (passed in constructor)
/// so existing direct-iteration code continues to work during migration.</summary>
public sealed partial class EntryListViewModel : ObservableObject, IEntryListViewModel, IEntryCardHost
{
    // -- shared collections —
    private readonly ObservableCollection<LauncherEntryViewModel> _entries;
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries;
    private readonly IEntryService _entryService;
    private readonly EntryManager _entryManager;
    private readonly Func<DateTimeOffset> _utcNow;

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }
    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisibleEmpty))]
    private int _visibleCount;

    public bool IsVisibleEmpty => VisibleCount == 0;

    public EntryListViewModel(
        ObservableCollection<LauncherEntryViewModel> entries,
        ObservableCollection<LauncherEntryViewModel> visibleEntries,
        IEntryService entryService,
        EntryManager entryManager,
        Func<DateTimeOffset> utcNow)
    {
        _entries = entries;
        _visibleEntries = visibleEntries;
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _entryManager = entryManager ?? throw new ArgumentNullException(nameof(entryManager));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        TotalCount = _entries.Count;
    }

    // ── public CRUD (IEntryListViewModel) ──

    public async Task AddEntryAsync()
    {
        _entryManager.DragPreviewDiscarder();
        var result = await _entryService.AddEntryAsync(
            _entries.Select(ToLaunchEntry), _entryManager.SettingsGetter(), _entryManager.CommandTemplatesGetter(),
            _entryManager.ReferenceCounter(""), _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                HandleAddOrCloneResult(success.Value, static e => LauncherEntryStatus.Added(e.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                _entryManager.StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                _entryManager.StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        _entryManager.DragPreviewDiscarder();
        var result = await _entryService.CloneEntryAsync(
            item, _entries.Select(e => e.Id), _entries.Select(ToLaunchEntry),
            _entryManager.SettingsGetter(), _entryManager.CommandTemplatesGetter(),
            _entryManager.ReferenceCounter(item.Id), _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                HandleAddOrCloneResult(success.Value, static e =>
                    LauncherEntryStatus.Added(e.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                _entryManager.StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                _entryManager.StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        _entryManager.DragPreviewDiscarder();
        var result = await _entryService.EditEntryAsync(
            item, _entries.Select(ToLaunchEntry), _entryManager.PresetFileGetter(), _entryManager.SettingsGetter(),
            _entryManager.CommandTemplatesGetter(), _entryManager.ReferenceCounter(item.Id),
            _utcNow).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                await HandleEditResult(item, success.Value);
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                _entryManager.StatusApplier(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                _entryManager.StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        _entryManager.DragPreviewDiscarder();
        var result = await _entryService.DeleteEntryAsync(
            item, _entryManager.KeyboardFocusedEntryId).ConfigureAwait(true);
        EntryDeleteApproval approval;
        switch (result)
        {
            case ServiceSuccess<EntryDeleteApproval> success:
                approval = success.Value;
                break;
            case ServiceCanceled<EntryDeleteApproval> canceled:
                _entryManager.StatusApplier(canceled.Status);
                return;
            case ServiceFailure<EntryDeleteApproval> failure:
                _entryManager.StatusApplier(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }

        if (approval.ClearKeyboardFocusId is not null)
            _entryManager.KeyboardFocusedEntryId = null;

        var target = approval.Target;
        var entrySnapshot = _entries.Select(ToLaunchEntry).ToImmutableList();
        var shellSnapshot = _entryManager.SnapshotCapturer();
        var settings = _entryManager.SettingsGetter();
        var presetFile = _entryManager.PresetFileGetter();

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(
            presetFile, settings.CustomEntryOrder, target.EntryId);
        var newSettings = settings with { CustomEntryOrder = updatedOrder };

        _entries.Remove(item);

        var error = _entryManager.ChangesApplier(_entries.Select(ToLaunchEntry), newSettings, updatedPresetFile);
        if (error is not null)
        {
            // Restore entries + shell state
            ReplaceEntries(entrySnapshot);
            _entryManager.SnapshotRestorer(shellSnapshot);
            _entryManager.StatusApplier(LauncherStatusContent.Error(error));
            return;
        }

        TotalCount = _entries.Count;
        _entryManager.StatusApplier(LauncherEntryStatus.Deleted(target.EntryName));
        WeakReferenceMessenger.Default.Send(new EntryDeletedMessage(target.EntryId));
    }

    public Task DiscoverAgentsAsync()
    {
        // Discovery is orchestrated by shell — delegate via bridge
        // ponytail: discovery does a separate store reload; kept in shell for now.
        return Task.CompletedTask;
    }

    /// <summary>Replace all entries, creating ViewModels that own this
    /// EntryListViewModel as their <see cref="IEntryCardHost"/>.</summary>
    public void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
            _entries.Add(new LauncherEntryViewModel(entry, this));
        TotalCount = _entries.Count;
    }

    // ── internal helpers ──

    private void HandleAddOrCloneResult(LaunchEntry entry, Func<LaunchEntry, LauncherStatusContent> statusFactory)
    {
        var entrySnapshot = _entries.Select(ToLaunchEntry).ToImmutableList();
        var shellSnapshot = _entryManager.SnapshotCapturer();
        var settings = _entryManager.SettingsGetter();
        var presetFile = _entryManager.PresetFileGetter();

        var vm = new LauncherEntryViewModel(entry, this);
        _entries.Add(vm);
        var newSettings = settings with
        {
            CustomEntryOrder = settings.CustomEntryOrder.Add(entry.Id),
        };

        var error = _entryManager.ChangesApplier(_entries.Select(ToLaunchEntry), newSettings, presetFile);
        if (error is not null)
        {
            _entries.Remove(vm);
            _entryManager.SnapshotRestorer(shellSnapshot);
            _entryManager.StatusApplier(LauncherStatusContent.Error(error));
            return;
        }

        TotalCount = _entries.Count;
        _entryManager.StatusApplier(statusFactory(entry));
        WeakReferenceMessenger.Default.Send(new EntryAddedMessage(entry));
    }

    private async Task HandleEditResult(LauncherEntryViewModel item, LaunchEntry updated)
    {
        var oldId = item.Id;
        var entrySnapshot = _entries.Select(ToLaunchEntry).ToImmutableList();
        var shellSnapshot = _entryManager.SnapshotCapturer();
        var settings = _entryManager.SettingsGetter();
        var presetFile = _entryManager.PresetFileGetter();

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
                _entryManager.SnapshotRestorer(shellSnapshot);
                _entryManager.StatusApplier(LauncherEntryStatus.EditDialogFailed(item.Name, ex));
                return;
            }
        }

        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());

        var error = _entryManager.ChangesApplier(_entries.Select(ToLaunchEntry), settings, presetFile);
        if (error is not null)
        {
            ReplaceEntries(entrySnapshot);
            _entryManager.SnapshotRestorer(shellSnapshot);
            _entryManager.StatusApplier(LauncherStatusContent.Error(error));
            return;
        }

        _entryManager.StatusApplier(LauncherEntryStatus.Updated(item.Name));
        WeakReferenceMessenger.Default.Send(new EntryUpdatedMessage(oldId, updated.Id, updated));
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel e) => e.SnapshotEntry();

    // ── IEntryCardHost ──

    bool IEntryCardHost.IsDetailedListMode => _entryManager.DetailedListModeChecker();
    bool IEntryCardHost.IsNamesViewMode => _entryManager.NamesModeChecker();

    void IEntryCardHost.UpdateSelection(LauncherEntryViewModel entry, bool selected) =>
        _entryManager.SelectionUpdater(entry, selected);

    void IEntryCardHost.ShowDryRun(LauncherEntryViewModel entry) =>
        _entryManager.DryRunInitiator(entry);

    Task IEntryCardHost.LaunchAsync(LauncherEntryViewModel entry) =>
        _entryManager.AsyncLauncher(entry);

    Task IEntryCardHost.EditEntryAsync(LauncherEntryViewModel entry) =>
        EditEntryAsync(entry);

    Task IEntryCardHost.DeleteEntryAsync(LauncherEntryViewModel entry) =>
        DeleteEntryAsync(entry);

    Task IEntryCardHost.CloneEntryAsync(LauncherEntryViewModel entry) =>
        CloneEntryAsync(entry);

    void IEntryCardHost.MoveEntryUp(LauncherEntryViewModel entry) =>
        _entryManager.EntryMoverUp(entry);

    bool IEntryCardHost.CanMoveEntryUp(LauncherEntryViewModel entry) =>
        _entryManager.EntryMoverUpChecker(entry);

    void IEntryCardHost.MoveEntryDown(LauncherEntryViewModel entry) =>
        _entryManager.EntryMoverDown(entry);

    bool IEntryCardHost.CanMoveEntryDown(LauncherEntryViewModel entry) =>
        _entryManager.EntryMoverDownChecker(entry);
}
