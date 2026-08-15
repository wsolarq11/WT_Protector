using System.Collections.Immutable;
using System.Diagnostics;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Entry command workflow: agent discovery apply and one-save entry collection
/// persistence shared by add/clone/edit/delete flows.
/// </summary>
public sealed partial class LauncherEntryCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly IEntryService _entryService;
    private readonly ICommandHost _host;

    public LauncherEntryCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        IEntryService entryService,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public bool SaveEntryCollectionChanges()
    {
        _host.SyncTotalCount();
        _host.EnsureCustomOrderContainsEntries();

        using var transaction = new StoreMutationSession(_host.SaveStoreFile, _host.RestoreVmStateFromStore);
        var saveFailure = transaction.Commit(_host.BuildStoreFile());
        if (saveFailure is null)
        {
            _context.SynchronizeProjections(ProjectionSyncMode.Mutation);
            return true;
        }

        _context.Status.Apply(LauncherStatusContent.Error(saveFailure));
        return false;
    }

    public void AddExistingDirectoryEntry(EntryDraft draft)
    {
        var result = _entryService.CreateEntryFromDraft(
            draft, originalId: null, previousEntry: null, _entries.MutableEntries.Select(ToLaunchEntry));
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var entry = success.Value with { UpdatedAtUtc = _context.GetTimestampUtc() };
                _entries.MutableEntries.Add(_host.CreateEntryViewModel(entry));
                _host.EnsureCustomOrderContainsEntries();
                if (SaveEntryCollectionChanges())
                    ApplyStatus(LauncherEntryStatus.Added(entry.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                ApplyStatus(canceled.Status);
                return;
            case ServiceFailure<LaunchEntry> failure:
                ApplyStatus(failure.Status);
                return;
            default:
                throw new UnreachableException();
        }
    }

    public bool TryAddDiscoveredAgents(
        IReadOnlyList<DiscoveredAgent> selected,
        string userHome,
        out int added,
        out IReadOnlyList<string> skipped)
    {
        added = 0;
        var skippedEntries = new List<string>();
        foreach (var discovered in selected)
        {
            var draft = AgentRecommendationFactory.ToDraft(discovered, userHome);
            var result = _entryService.CreateEntryFromDraft(
                draft, originalId: null, previousEntry: null, _entries.MutableEntries.Select(ToLaunchEntry));
            if (result is not ServiceSuccess<LaunchEntry> success)
            {
                ApplyStatus(result switch
                {
                    ServiceCanceled<LaunchEntry> canceled => canceled.Status,
                    ServiceFailure<LaunchEntry> failure => failure.Status,
                    _ => throw new UnreachableException(),
                });
                skippedEntries.Add(discovered.Catalog.DisplayName);
                continue;
            }

            var entry = success.Value with { UpdatedAtUtc = _context.GetTimestampUtc() };
            _entries.MutableEntries.Add(_host.CreateEntryViewModel(entry));
            added++;
        }

        _host.EnsureCustomOrderContainsEntries();
        skipped = skippedEntries;
        return SaveEntryCollectionChanges();
    }

    private void ApplyStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            _context.Status.Apply(status);
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();
}
