using System.Collections.Frozen;
using System.Collections.ObjectModel;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetSelectionPreview
{
    public static string DefaultApplyButtonText => LauncherLocalization.Format("presetPreview.applyDefault");

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
                ? LauncherLocalization.Format("presetPreview.none")
                : LauncherLocalization.Format("presetPreview.select"));
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
                LauncherLocalization.Format("presetPreview.missingTitle"),
                LauncherLocalization.Format("presetPreview.missingMessage", presetName, missingConfigIds.Count));
        }

        if (presetSet.Count == 0)
        {
            return Create(
                extra == 0 ? LauncherLocalization.Format("presetPreview.manualMode") : LauncherLocalization.Format("presetPreview.clearSelection"),
                extra == 0
                    ? LauncherLocalization.Format("presetPreview.already", presetName)
                    : LauncherLocalization.Format("presetPreview.willClear", presetName, extra));
        }

        var isMatched = missing == 0 && extra == 0;
        return Create(
            isMatched ? LauncherLocalization.Format("presetPreview.matched") : DefaultApplyButtonText,
            isMatched
                ? LauncherLocalization.Format("presetPreview.matchedStatus", presetName, matched, presetSet.Count)
                : LauncherLocalization.Format("presetPreview.partialStatus", presetName, matched, presetSet.Count, missing, extra));
    }

}
