namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetSelectionPreview
{
    private const string ApplyPresetText = "应用启动组";

    private LauncherPresetSelectionPreview(
        IReadOnlySet<string> selectedIds,
        IReadOnlySet<string> presetIds,
        IReadOnlyList<string> missingConfigIds,
        int matchedCount,
        int missingCount,
        int extraCount,
        string applyButtonText,
        string statusText)
    {
        SelectedIds = selectedIds;
        PresetIds = presetIds;
        MissingConfigIds = missingConfigIds;
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
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            [],
            matchedCount: 0,
            missingCount: 0,
            extraCount: 0,
            ApplyPresetText,
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
        var selectedSet = ToOrdinalSet(selectedIds);
        var knownSet = ToOrdinalSet(knownIds);
        var presetSet = ToOrdinalSet(presetIds);
        var missingConfigIds = presetSet.Where(id => !knownSet.Contains(id)).ToList();
        var matched = selectedSet.Count(presetSet.Contains);
        var missing = presetSet.Count - matched;
        var extra = selectedSet.Count - matched;

        if (missingConfigIds.Count > 0)
        {
            return new LauncherPresetSelectionPreview(
                selectedSet,
                presetSet,
                missingConfigIds,
                matched,
                missing,
                extra,
                "检查启动组",
                $"启动组“{presetName}”引用了 {missingConfigIds.Count} 个不存在的启动项；请先修 config\\presets.json。");
        }

        if (presetSet.Count == 0)
        {
            return new LauncherPresetSelectionPreview(
                selectedSet,
                presetSet,
                missingConfigIds,
                matched,
                missing,
                extra,
                extra == 0 ? "手动模式" : "清空勾选",
                extra == 0
                    ? $"已是“{presetName}”：当前没有勾选项。"
                    : $"“{presetName}”会清空当前 {extra} 个勾选项。");
        }

        return new LauncherPresetSelectionPreview(
            selectedSet,
            presetSet,
            missingConfigIds,
            matched,
            missing,
            extra,
            missing == 0 && extra == 0 ? "已匹配" : ApplyPresetText,
            missing == 0 && extra == 0
                ? $"已匹配启动组“{presetName}”：{matched}/{presetSet.Count} 项。"
                : $"启动组“{presetName}”：已匹配 {matched}/{presetSet.Count}，缺 {missing}，额外 {extra}；应用会替换当前勾选。");
    }

    private static HashSet<string> ToOrdinalSet(IEnumerable<string> ids)
    {
        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
    }
}
