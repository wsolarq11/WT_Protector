using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetCatalog
{
    public static LauncherPresetViewModel? Apply(
        LauncherPresetFile presetFile,
        ObservableCollection<LauncherPresetViewModel> presets,
        string? preferredPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presets);

        presets.Clear();
        foreach (var preset in presetFile.Presets)
        {
            presets.Add(new LauncherPresetViewModel(preset));
        }

        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        return presets.FirstOrDefault(preset => preset.Id == targetPresetId)
            ?? presets.FirstOrDefault();
    }

    public static LauncherPresetSelectionPreview BuildSelectionPreview(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<string> selectedEntryIds,
        IEnumerable<string> allEntryIds)
    {
        if (selectedPreset is null)
            return LauncherPresetSelectionPreview.NoPreset(presetCount);

        return LauncherPresetSelectionPreview.ForPreset(
            selectedPreset.Name,
            selectedEntryIds,
            allEntryIds,
            selectedPreset.EntryIds);
    }

    public static List<string> GetSelectedEntryIds(IEnumerable<LauncherEntryViewModel> entries)
    {
        return entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToList();
    }
}
