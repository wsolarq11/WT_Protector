using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherReloadWorkflow
{
    public static LauncherReloadResult Load(
        string configPath,
        string presetPath,
        ObservableCollection<LauncherEntryViewModel> entries,
        ObservableCollection<LauncherPresetViewModel> presets,
        List<string> customEntryOrder,
        Func<LaunchEntry, LauncherEntryViewModel> createViewModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(presetPath);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(createViewModel);

        var entrySummary = LauncherEntryReload.Load(configPath, entries, createViewModel);
        LauncherEntryOrder.SyncWithEntries(customEntryOrder, entries.Select(entry => entry.Id));

        var presetResult = LauncherPresetReload.Load(presetPath, presets, preferredPresetId: null);
        return new LauncherReloadResult(
            entrySummary.TotalCount,
            entrySummary.SelectedCount,
            presetResult.PresetFile,
            presetResult.SelectedPreset,
            presetResult.PresetCount,
            presetResult.ErrorMessage
            ?? $"已加载 {entrySummary.TotalCount} 个启动项、{presetResult.PresetCount} 个启动组；勾选自动保存到 config\\config.json");
    }
}

public sealed record LauncherReloadResult(
    int TotalCount,
    int SelectedCount,
    LauncherPresetFile PresetFile,
    LauncherPresetViewModel? SelectedPreset,
    int PresetCount,
    string StatusText);
