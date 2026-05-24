namespace WTLauncher.App.ViewModels;

public static class LauncherPresetApplyWorkflow
{
    public static LauncherPresetApplyResult Apply(
        LauncherPresetViewModel? selectedPreset,
        int presetCount,
        IEnumerable<LauncherEntryViewModel> entries,
        Func<IReadOnlySet<string>, bool> replaceSelection)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(replaceSelection);

        if (selectedPreset is null)
            return LauncherPresetApplyResult.Status("未选择启动组");

        var selection = LauncherPresetSelectionRules.BuildPreview(selectedPreset, presetCount, entries);
        if (selection.MissingConfigIds.Count > 0)
        {
            return LauncherPresetApplyResult.Status(
                $"启动组引用了不存在的启动项: {string.Join(", ", selection.MissingConfigIds)}");
        }

        return replaceSelection(selection.PresetIds)
            ? LauncherPresetApplyResult.Status($"已应用启动组: {selectedPreset.Name}（{selection.PresetIds.Count} 项，已保存）")
            : LauncherPresetApplyResult.NoChange();
    }
}

public readonly record struct LauncherPresetApplyResult(string? StatusText)
{
    public static LauncherPresetApplyResult NoChange()
    {
        return new LauncherPresetApplyResult(null);
    }

    public static LauncherPresetApplyResult Status(string statusText)
    {
        return new LauncherPresetApplyResult(statusText);
    }
}
