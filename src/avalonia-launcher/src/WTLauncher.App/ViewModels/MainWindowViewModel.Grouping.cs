using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Cards 视图分组渲染。Split from MainWindowViewModel.cs (task 07-11-cards-command-grouping).
// VisibleGroups 是渲染源 — groupKey=none 时单组"all"无 header;非 none 时按 key 分组。
// ponytail: 单源真 — VisibleEntries 是 canonical,VisibleGroups 是它的派生 view projection。

public sealed partial class MainWindowViewModel
{
    private string _cardsGroupKey = LauncherCardsGroupKeys.None;

    /// <summary>ComboBox 选项 — 单次构造,运行时不可变。ponytail: 与 DisplayModes 同模式。</summary>
    public IReadOnlyList<LauncherCardsGroupOption> CardsGroupOptions { get; } = new[]
    {
        new LauncherCardsGroupOption("无分组", LauncherCardsGroupKeys.None),
        new LauncherCardsGroupOption("按 command", LauncherCardsGroupKeys.Command),
        new LauncherCardsGroupOption("按 directory", LauncherCardsGroupKeys.Directory),
    };

    public LauncherCardsGroupOption? SelectedCardsGroupOption
    {
        get => CardsGroupOptions.FirstOrDefault(o => string.Equals(o.Value, _cardsGroupKey, StringComparison.Ordinal));
        set
        {
            if (value is not null)
                SetCardsGroupKey(value.Value);
        }
    }

    public string CardsGroupKey
    {
        get => _cardsGroupKey;
        set => SetCardsGroupKey(value);
    }

    /// <summary>Cards 视图渲染源。groupKey=none 时含 1 组(showHeader=false),非 none 时按 key 分组。</summary>
    public ObservableCollection<LauncherCardGroupViewModel> VisibleGroups { get; } = new();

    private void SetCardsGroupKey(string value)
    {
        var normalized = LauncherCardsGroupKeys.Normalize(value);
        if (string.Equals(_cardsGroupKey, normalized, StringComparison.Ordinal))
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        _cardsGroupKey = normalized;
        _settings = _settings with { CardsGroupKey = normalized };
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            RestoreEntryMutationSnapshot(snapshot);
            _cardsGroupKey = _settings.CardsGroupKey;
            StatusText = saveFailure;
            return;
        }

        OnPropertyChanged(nameof(CardsGroupKey));
        OnPropertyChanged(nameof(SelectedCardsGroupOption));
        RebuildVisibleGroups();
    }

    /// <summary>从 VisibleEntries 派生 VisibleGroups。每次 ApplyFilter / Refresh / group key 变更后调用。</summary>
    internal void RebuildVisibleGroups()
    {
        var launchEntries = _visibleEntries.Select(ToLaunchEntry).ToList();
        var groups = LauncherEntryGrouper.Group(launchEntries, _cardsGroupKey);
        // ponytail: TryAdd 而非 ToDictionary — 与 LaunchBatchAsync 同模式,_entries 出现重复 ID 时(仅可能由反射注入或 schema 历史损坏)优雅退化,不抛 ArgumentException。
        var entryById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
        foreach (var vm in _visibleEntries)
            entryById.TryAdd(vm.Id, vm);

        VisibleGroups.Clear();
        var showHeader = !string.Equals(_cardsGroupKey, LauncherCardsGroupKeys.None, StringComparison.Ordinal);
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
            StatusText = LauncherRefreshStatus.GroupNoEntries;
            return;
        }

        await LaunchBatchAsync(
            entries,
            confirmationTargetLabel: $"分组 \"{entries[0].Command}\"",
            emptyStatus: "组内无 entry。",
            canceledStatus: _ => "已取消批量启动",
            failedStatus: ex => $"批量启动失败: {ExceptionMessageFormatter.ForUser(ex)}",
            completedStatus: report => $"已启动组内 {report.RequestedCount} 个 entry。").ConfigureAwait(true);
    }

    private async Task SaveGroupAsPresetAsync(IReadOnlyList<string> entryIds)
    {
        if (entryIds.Count == 0)
        {
            StatusText = LauncherRefreshStatus.GroupNoEntriesCannotSave;
            return;
        }

        DiscardDragReorderPreview();
        var target = new LauncherPresetSaveTarget(entryIds);
        string? presetName;
        try
        {
            presetName = await RequestNewPresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Grouping.SaveAsPreset");
            StatusText = LauncherRefreshStatus.SaveGroupFailed(ex);
            return;
        }

        if (presetName is null)
        {
            StatusText = LauncherRefreshStatus.DialogCanceled("保存启动组");
            return;
        }

        var snapshot = CapturePresetMutationSnapshot();
        LauncherPreset? preset;
        try
        {
            preset = LauncherPresetWorkflow.AddPreset(ref _presetFile, presetName, target.EntryIds);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Grouping.AddPreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = LauncherRefreshStatus.AddGroupFailed(ex);
            return;
        }
        if (SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset) && savedPreset is not null)
            ApplyStatusText(LauncherPresetStatus.Saved(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    // ponytail: ApplyFilter 末尾追加 RebuildVisibleGroups 调用,见 MainWindowViewModel.cs 主类。
    // Refresh() 末尾同样追加。
}
