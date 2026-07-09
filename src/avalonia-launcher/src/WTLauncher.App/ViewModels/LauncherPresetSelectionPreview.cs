using System.Collections.Frozen;
using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetSelectionPreview
{
    public static string DefaultApplyButtonText => "应用启动组";

    private LauncherPresetSelectionPreview(
        IEnumerable<string> selectedIds,
        IEnumerable<string> presetIds,
        IEnumerable<string> missingConfigIds,
        int matchedCount,
        int missingCount,
        int extraCount,
        string applyButtonText,
        string statusText)
    {
        SelectedIds = selectedIds.ToFrozenSet(EntryId.Comparer);
        PresetIds = presetIds.ToFrozenSet(EntryId.Comparer);
        MissingConfigIds = new ReadOnlyCollection<string>([.. missingConfigIds]);
        MatchedCount = matchedCount;
        MissingCount = missingCount;
        ExtraCount = extraCount;
        ApplyButtonText = applyButtonText;
        StatusText = statusText;
    }

    public IReadOnlySet<string> SelectedIds { get; }

    public IReadOnlySet<string> PresetIds { get; }

    public IReadOnlyList<string> MissingConfigIds { get; }

    public int MatchedCount { get; }

    public int MissingCount { get; }

    public int ExtraCount { get; }

    public string ApplyButtonText { get; }

    public string StatusText { get; }

    public bool HasChanges => !SelectedIds.SetEquals(PresetIds);

    public static LauncherPresetSelectionPreview NoPreset(int presetCount)
    {
        return new LauncherPresetSelectionPreview(
            [],
            [],
            [],
            matchedCount: 0,
            missingCount: 0,
            extraCount: 0,
            DefaultApplyButtonText,
            presetCount == 0
                ? "暂无启动组；可以继续手动勾选。"
                : "请选择一个启动组查看差异。");
    }

    public static LauncherPresetSelectionPreview ForPreset(
        string presetName,
        IEnumerable<string> selectedIds,
        IEnumerable<string> knownIds,
        IEnumerable<string> presetIds)
    {
        var selectedSet = EntryId.ToRequiredSet(selectedIds, nameof(selectedIds));
        var knownSet = EntryId.ToRequiredSet(knownIds, nameof(knownIds));
        var presetSet = EntryId.ToRequiredSet(presetIds, nameof(presetIds));
        var missingConfigIds = presetSet.Where(id => !knownSet.Contains(id)).ToList();
        var matched = selectedSet.Count(presetSet.Contains);
        var missing = presetSet.Count - matched;
        var extra = selectedSet.Count - matched;

        LauncherPresetSelectionPreview Create(string applyButtonText, string statusText) =>
            new(
                selectedSet,
                presetSet,
                missingConfigIds,
                matched,
                missing,
                extra,
                applyButtonText,
                statusText);

        if (missingConfigIds.Count > 0)
        {
            return Create(
                "检查启动组",
                $"启动组“{presetName}”引用了 {missingConfigIds.Count} 个不存在的启动项；请先修 config\\launcher.json。");
        }

        if (presetSet.Count == 0)
        {
            return Create(
                extra == 0 ? "手动模式" : "清空勾选",
                extra == 0
                    ? $"已是“{presetName}”：当前没有勾选项。"
                    : $"“{presetName}”会清空当前 {extra} 个勾选项。");
        }

        var isMatched = missing == 0 && extra == 0;
        return Create(
            isMatched ? "已匹配" : DefaultApplyButtonText,
            isMatched
                ? $"已匹配启动组“{presetName}”：{matched}/{presetSet.Count} 项。"
                : $"启动组“{presetName}”：已匹配 {matched}/{presetSet.Count}，缺 {missing}，额外 {extra}；应用会替换当前勾选。");
    }

}
