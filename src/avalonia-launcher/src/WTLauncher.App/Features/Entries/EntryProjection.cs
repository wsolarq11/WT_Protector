using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>
/// Zone 1: Declarative entry projection using DynamicData.
/// Filter and sort changes propagate automatically — no manual ApplyFilter needed.
/// </summary>
public sealed class EntryProjection : IDisposable
{
    private readonly SourceCache<LauncherEntryViewModel, string> _cache =
        new(static vm => vm.Id);

    private readonly BehaviorSubject<string> _filterSubject = new("");
    private readonly BehaviorSubject<string> _sortModeSubject = new("custom");

    private readonly IDisposable _cleanup;
#pragma warning disable IDE0044 // Assigned via Bind(out ...) in constructor
    private ReadOnlyObservableCollection<LauncherEntryViewModel> _visibleEntries = null!;
#pragma warning restore IDE0044

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries => _visibleEntries;
    public int TotalCount => _cache.Count;

    public EntryProjection()
    {
        var filterPredicate = _filterSubject
            .DistinctUntilChanged()
            .Select<string, Func<LauncherEntryViewModel, bool>>(query =>
            {
                if (string.IsNullOrWhiteSpace(query))
                    return _ => true;
                var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return vm =>
                {
                    var e = vm.SnapshotEntry();
                    return tokens.All(t =>
                        e.Name.ToString()!.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        e.Directory.ToString().Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        e.Command.Contains(t, StringComparison.OrdinalIgnoreCase));
                };
            });

        var sortComparer = _sortModeSubject
            .DistinctUntilChanged()
            .Select(mode =>
            {
                var normalized = LauncherEntryQuery.NormalizeSortModeId(mode);
                return normalized switch
                {
                    "recent" => SortExpressionComparer<LauncherEntryViewModel>
                        .Descending(vm => vm.SnapshotEntry().LaunchedAtUtc),
                    "updated" => SortExpressionComparer<LauncherEntryViewModel>
                        .Descending(vm => vm.SnapshotEntry().UpdatedAtUtc),
                    "alphabetical" => SortExpressionComparer<LauncherEntryViewModel>
                        .Ascending(vm => vm.SnapshotEntry().Name.ToString()),
                    _ => SortExpressionComparer<LauncherEntryViewModel>
                        .Ascending(vm => vm.SnapshotEntry().Name.ToString())
                };
            });

        // DynamicData pipeline: Filter → Sort → Bind
        // ObserveOn SynchronizationContext only in production (UI thread).
        // In tests (null context), run inline.
        var pipeline = _cache.Connect()
            .Filter(filterPredicate)
            .Sort(sortComparer, SortOptimisations.ComparesImmutableValuesOnly);

        var syncCtx = SynchronizationContext.Current;
        if (syncCtx is not null)
            pipeline = pipeline.ObserveOn(syncCtx);

        _cleanup = pipeline
            .Bind(out _visibleEntries)
            .DisposeMany()
            .Subscribe();
    }

    public void LoadEntries(IEnumerable<LaunchEntry> entries, IEntryCardHost host)
    {
        var viewModels = entries.Select(e => new LauncherEntryViewModel(e, host));
        _cache.EditDiff(viewModels, (a, b) => a.Id == b.Id);
    }

    public void SetFilter(string text) => _filterSubject.OnNext(text ?? "");
    public void SetSortMode(string mode) => _sortModeSubject.OnNext(mode ?? "custom");
    public void AddOrUpdate(LauncherEntryViewModel vm) => _cache.AddOrUpdate(vm);
    public void Remove(string id) => _cache.Remove(id);
    public IReadOnlyList<LauncherEntryViewModel> GetAllEntries() => _cache.Items.ToList();
    public void Dispose()
    {
        _cleanup.Dispose();
        _cache.Dispose();
        _filterSubject.Dispose();
        _sortModeSubject.Dispose();
    }
}
