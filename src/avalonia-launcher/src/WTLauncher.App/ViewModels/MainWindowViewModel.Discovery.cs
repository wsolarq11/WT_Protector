using System.Collections.Immutable;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Discovery dialog orchestration surface. Discovery apply and collection save
// live in LauncherEntryCommandWorkflow.

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
                CurrentSettings.CommandTemplates)
                .ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Dialog");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed(LauncherLocalization.Format("dialog.discover"), ex);
            return;
        }

        if (selection is null)
        {
            StatusContent = LauncherRefreshStatus.DialogCanceled(LauncherLocalization.Format("dialog.discover"));
            return;
        }

        if (selection is ExistingDirectorySelection directorySelection)
        {
            _entryWorkflow.AddExistingDirectoryEntry(directorySelection.Draft);
            return;
        }

        var selected = selection switch
        {
            DiscoveredAgentsSelection agents => agents.Agents,
            _ => throw new UnreachableException(),
        };
        if (selected.Count == 0)
        {
            StatusContent = LauncherRefreshStatus.DialogCanceled(LauncherLocalization.Format("dialog.discover"));
            return;
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!_entryWorkflow.TryAddDiscoveredAgents(selected, userHome, out var added, out var skipped))
            return;

        StatusContent = LauncherDiscoveryStatus.AgentsAdded(added, skipped);
    }
}
