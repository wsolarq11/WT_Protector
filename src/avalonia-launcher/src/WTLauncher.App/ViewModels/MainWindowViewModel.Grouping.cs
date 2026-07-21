using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Cards 视图分组渲染。Split from MainWindowViewModel.cs (task 07-11-cards-command-grouping).
// VisibleGroups 是渲染源 — groupKey=none 时单组"all"无 header;非 none 时按 key 分组。
// ponytail: 单源真 — VisibleEntries 是 canonical,VisibleGroups 是它的派生 view projection。

public sealed partial class MainWindowViewModel
{
    /// <summary>Cards 视图渲染源。groupKey=none 时含 1 组(showHeader=false),非 none 时按 key 分组。</summary>
    public ObservableCollection<LauncherCardGroupViewModel> VisibleGroups { get; } = new();

    /// <summary>从 VisibleEntries 派生 VisibleGroups。每次 ApplyFilter / Refresh / group key 变更后调用。</summary>
    internal void RebuildVisibleGroups()
    {
        var launchEntries = _list.MutableVisibleEntries.Select(ToLaunchEntry).ToList();
        var groups = LauncherEntryGrouper.Group(launchEntries, _layoutManager.CardsGroupKey);
        // ponytail: TryAdd 而非 ToDictionary — 与 LaunchBatchAsync 同模式,_entries 出现重复 ID 时(仅可能由反射注入或 schema 历史损坏)优雅退化,不抛 ArgumentException。
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

            var displayName = showHeader ? g.Key : string.Empty;
            VisibleGroups.Add(new LauncherCardGroupViewModel(
                key: g.Key,
                displayName: displayName,
                count: g.Count,
                showHeader: showHeader,
                entries: entriesInGroup,
                launchAllAsync: LaunchGroupAsync,
                saveAsPresetAsync: SaveGroupAsPresetAsync));
        }
    }

    private async Task LaunchGroupAsync(IReadOnlyList<LauncherEntryViewModel> entries)
    {
        if (entries.Count == 0)
        {
            StatusContent = LauncherRefreshStatus.GroupNoEntries;
            return;
        }

        await _launchOrchestrator.LaunchBatchAsync(
            entries,
            confirmationTargetLabel: $"分组 \"{entries[0].Command}\"",
            emptyStatus: LauncherRefreshStatus.GroupNoEntries,
            canceledStatus: _ => LauncherRefreshStatus.CanceledBatch,
            failedStatus: LauncherSelectionStatus.BatchLaunchFailed,
            completedStatus: LauncherGroupStatus.BatchCompleted).ConfigureAwait(true);
    }

    private async Task SaveGroupAsPresetAsync(IReadOnlyList<string> entryIds)
    {
        if (entryIds.Count == 0)
        {
            StatusContent = LauncherRefreshStatus.GroupNoEntriesCannotSave;
            return;
        }

        DiscardDragReorderPreview();
        var target = new LauncherPresetSaveTarget(entryIds);
        var nameResult = await _presetService.RequestNewPresetAsync(target, _presetManager.Presets.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)).ConfigureAwait(true);
        if (nameResult is not ServiceSuccess<string> nameSuccess)
        {
            StatusContent = nameResult switch
            {
                ServiceCanceled<string> canceled => canceled.Status,
                ServiceFailure<string> failure => failure.Status,
                _ => throw new UnreachableException(),
            };
            return;
        }
        var presetName = nameSuccess.Value;

        var snapshot = _presetManager.CaptureSnapshot();
        var addResult = _presetService.AddPreset(_presetManager.File, presetName, target.EntryIds);
        if (addResult is not ServiceSuccess<PresetMutation> addSuccess)
        {
            _presetManager.Load(snapshot.File, snapshot.SelectedPresetId);
            StatusContent = addResult switch
            {
                ServiceCanceled<PresetMutation> canceled => canceled.Status,
                ServiceFailure<PresetMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            };
            return;
        }
        var preset = addSuccess.Value.Preset;
        if (SavePresetMutationAndApply(snapshot, addSuccess.Value.PresetFile, preset.Id, out var savedPreset) && savedPreset is not null)
            ApplyStatusText(LauncherPresetStatus.Saved(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    // ponytail: ApplyFilter 末尾追加 RebuildVisibleGroups 调用,见 MainWindowViewModel.cs 主类。
    // Refresh() 末尾同样追加。
}
