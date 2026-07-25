using System.Collections.Immutable;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Discovery Add flow. Split from MainWindowViewModel.cs as part of
// 07-10-agent-cli-discovery. Uses IDialogService for dialog interaction.

public sealed partial class MainWindowViewModel
{

    [RelayCommand]
    private async Task DiscoverAgentsAsync()
    {
        DiscardDragReorderPreview();

        DiscoveryDialogResult? selection;
        try
        {
            selection = await _dialogService.DiscoverAgentsAsync(
                Entries
                    .Select(e => (e.Command ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty)
                    .Select(token => Path.GetFileNameWithoutExtension(token))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                DirectoryUsageAnalyzer.Analyze(_list.MutableEntries.Select(ToLaunchEntry)),
                CurrentSettings.CommandTemplates) // Phase 1
                .ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Dialog");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("发现", ex);
            return;
        }

        if (selection is null)
        {
            StatusContent = LauncherRefreshStatus.DialogCanceled("发现");
            return;
        }

        if (selection is ExistingDirectorySelection directorySelection)
        {
            AddExistingDirectoryEntry(directorySelection.Draft);
            return;
        }

        var selected = selection switch
        {
            DiscoveredAgentsSelection agents => agents.Agents,
            _ => throw new UnreachableException(),
        };
        if (selected.Count == 0)
        {
            StatusContent = LauncherRefreshStatus.DialogCanceled("发现");
            return;
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        int added = 0;
        var skipped = new List<string>();
        foreach (var discovered in selected)
        {
            var draft = AgentRecommendationFactory.ToDraft(discovered, userHome);
            var result = _entryService.CreateEntryFromDraft(draft, originalId: null, previousEntry: null, _list.MutableEntries.Select(ToLaunchEntry));
            if (result is not ServiceSuccess<LaunchEntry> success)
            {
                StatusContent = result switch
                {
                    ServiceCanceled<LaunchEntry> canceled => canceled.Status,
                    ServiceFailure<LaunchEntry> failure => failure.Status,
                    _ => throw new UnreachableException(),
                };
                skipped.Add(discovered.Catalog.DisplayName);
                continue;
            }

            var entry = success.Value;
            entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
            _list.MutableEntries.Add(new LauncherEntryViewModel(entry, this));
            // CustomEntryOrder is synced via EnsureCustomOrderContainsEntries during apply
            added++;
        }

        EnsureCustomOrderContainsEntries();
        if (!SaveEntryCollectionChanges())
            return;

        StatusContent = LauncherDiscoveryStatus.AgentsAdded(added, skipped);
    }

    private void AddExistingDirectoryEntry(EntryDraft draft)
    {
        var result = _entryService.CreateEntryFromDraft(
            draft, originalId: null, previousEntry: null, _list.MutableEntries.Select(ToLaunchEntry));
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var entry = success.Value with { UpdatedAtUtc = CurrentTimestampUtc() };
                _list.MutableEntries.Add(new LauncherEntryViewModel(entry, this));
                // CustomEntryOrder is synced via EnsureCustomOrderContainsEntries during apply
                EnsureCustomOrderContainsEntries();
                if (SaveEntryCollectionChanges())
                    ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusContent = canceled.Status;
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusContent = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }
    }
}
