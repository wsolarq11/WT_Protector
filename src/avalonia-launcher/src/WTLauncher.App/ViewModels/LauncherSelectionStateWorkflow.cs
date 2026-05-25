namespace WTLauncher.App.ViewModels;

public static class LauncherSelectionStateWorkflow
{
    public static LauncherPresetSelectionState Refresh(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries,
        Action<LauncherPresetSelectionState> applyState,
        Action? notifySelectionCommands = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(applyState);

        var state = LauncherPresetSelectionRules.BuildState(selectedPreset, presetCount, entries);
        applyState(state);
        notifySelectionCommands?.Invoke();
        return state;
    }
}
