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
                DirectoryUsageAnalyzer.Analyze(_entries.Select(ToLaunchEntry)),
                _settings.CommandTemplates)
                .ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Dialog");
            StatusText = LauncherRefreshStatus.DialogOpenFailed("发现", ex);
            return;
        }

        if (selection is null)
        {
            StatusText = LauncherRefreshStatus.DialogCanceled("发现");
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
            StatusText = LauncherRefreshStatus.DialogCanceled("发现");
            return;
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var snapshot = CaptureEntryMutationSnapshot();

        int added = 0;
        var skipped = new List<string>();
        foreach (var discovered in selected)
        {
            var draft = AgentRecommendationFactory.ToDraft(discovered, userHome);
            var result = _entryService.CreateEntryFromDraft(draft, originalId: null, previousEntry: null, _entries.Select(ToLaunchEntry));
            if (result is not ServiceSuccess<LaunchEntry> success)
            {
                StatusText = result switch
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
            _entries.Add(new LauncherEntryViewModel(entry, this));
            _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };
            added++;
        }

        if (!SaveEntryCollectionChanges(snapshot))
            return;

        StatusText = skipped.Count == 0
            ? $"已添加 {added} 个 AI Agent 启动项"
            : $"已添加 {added} 个 AI Agent 启动项（{skipped.Count} 个跳过: {string.Join(", ", skipped)}）";
    }

    private void AddExistingDirectoryEntry(EntryDraft draft)
    {
        var result = _entryService.CreateEntryFromDraft(
            draft, originalId: null, previousEntry: null, _entries.Select(ToLaunchEntry));
        switch (result)
        {
            case ServiceSuccess<LaunchEntry> success:
                var snapshot = CaptureEntryMutationSnapshot();
                var entry = success.Value with { UpdatedAtUtc = CurrentTimestampUtc() };
                _entries.Add(new LauncherEntryViewModel(entry, this));
                _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };
                if (SaveEntryCollectionChanges(snapshot))
                    ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
                return;
            case ServiceCanceled<LaunchEntry> canceled:
                StatusText = canceled.Status;
                return;
            case ServiceFailure<LaunchEntry> failure:
                StatusText = failure.Status;
                return;
            default:
                throw new UnreachableException();
        }
    }
}
