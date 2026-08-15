using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Cards 视图分组渲染投影。Launch/save-as-preset bodies live in
// LauncherPresetCommandWorkflow; VisibleGroups is a derived projection.

public sealed partial class MainWindowViewModel
{
    public ObservableCollection<LauncherCardGroupViewModel> VisibleGroups { get; } = new();

    internal void RebuildVisibleGroups()
    {
        var launchEntries = _list.MutableVisibleEntries.Select(ToLaunchEntry).ToList();
        var groups = LauncherEntryGrouper.Group(launchEntries, _layoutManager.CardsGroupKey);
        var entryById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
        foreach (var vm in _list.MutableVisibleEntries)
            entryById.TryAdd(vm.Id, vm);

        VisibleGroups.Clear();
        var showHeader = !string.Equals(_layoutManager.CardsGroupKey, LauncherCardsGroupKeys.None, StringComparison.Ordinal);
        foreach (var g in groups)
        {
            var entriesInGroup = g.Entries
                .Select(e => entryById.TryGetValue(e.Id, out var vm) ? vm : null)
                .Where(vm => vm is not null)
                .Cast<LauncherEntryViewModel>()
                .ToList();

            VisibleGroups.Add(new LauncherCardGroupViewModel(
                key: g.Key,
                displayName: showHeader ? g.Key : string.Empty,
                count: g.Count,
                showHeader: showHeader,
                entries: entriesInGroup,
                launchAllAsync: _presetWorkflow.LaunchGroupAsync,
                saveAsPresetAsync: _presetWorkflow.SaveGroupAsPresetAsync));
        }
    }
}
