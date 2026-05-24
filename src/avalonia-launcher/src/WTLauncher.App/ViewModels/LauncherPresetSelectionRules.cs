using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetSelectionRules
{
    public static LauncherPresetSelectionState BuildState(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var entryList = entries as IList<LauncherEntryViewModel> ?? entries.ToList();
        var preview = BuildPreview(selectedPreset, presetCount, entryList);
        return new LauncherPresetSelectionState(
            entryList.Count(entry => entry.IsSelected),
            preview.ApplyButtonText,
            preview.StatusText);
    }

    public static LauncherPresetSelectionPreview BuildPreview(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var entryList = entries as IList<LauncherEntryViewModel> ?? entries.ToList();
        var selectedIds = entryList.Where(entry => entry.IsSelected).Select(entry => entry.Entry.Id);
        var allEntryIds = entryList.Select(entry => entry.Entry.Id);
        return LauncherPresetCatalog.BuildSelectionPreview(
            selectedPreset,
            presetCount,
            selectedIds,
            allEntryIds);
    }

    public static bool CanApply(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries)
    {
        if (selectedPreset is null)
            return false;
        var entryList = entries as IList<LauncherEntryViewModel> ?? entries.ToList();
        if (entryList.Count == 0)
            return false;

        return BuildPreview(selectedPreset, presetCount, entryList).HasChanges;
    }

    public static bool CanUpdate(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries)
    {
        if (selectedPreset is null || selectedPreset.IsLocked)
            return false;
        var entryList = entries as IList<LauncherEntryViewModel> ?? entries.ToList();
        if (entryList.Count == 0)
            return false;
        if (SelectedCount(entryList) == 0)
            return false;

        return BuildPreview(selectedPreset, presetCount, entryList).HasChanges;
    }

    public static bool CanDelete(
        LauncherPresetViewModel? selectedPreset,
        LauncherPresetFile presetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        if (selectedPreset is null || selectedPreset.IsLocked)
            return false;

        return LauncherPresetWorkflow.CanDeletePreset(presetFile, selectedPreset.Id);
    }

    private static int SelectedCount(IEnumerable<LauncherEntryViewModel> entries)
    {
        return entries.Count(entry => entry.IsSelected);
    }
}

public sealed record LauncherPresetSelectionState(
    int SelectedCount,
    string ApplyPresetButtonText,
    string PresetSelectionStatusText);
