namespace WTLauncher.App.ViewModels;

public static class LauncherSelectionWorkflow
{
    public static LauncherSelectionActionResult SelectVisible(
        IReadOnlyList<LauncherEntryViewModel> visibleEntries,
        Action refreshSelectionState,
        Func<bool> saveConfig)
    {
        ArgumentNullException.ThrowIfNull(visibleEntries);
        ArgumentNullException.ThrowIfNull(refreshSelectionState);
        ArgumentNullException.ThrowIfNull(saveConfig);

        var targets = visibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            return new LauncherSelectionActionResult(
                visibleEntries.Count == 0
                    ? "当前没有可勾选的可见项"
                    : "可见项已全部勾选");
        }

        var snapshot = targets
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
        foreach (var entry in targets)
        {
            entry.SetSelectionFromOwner(selected: true);
        }

        refreshSelectionState();
        if (saveConfig())
            return new LauncherSelectionActionResult($"已勾选可见 {targets.Count} 项（已保存）");

        RestoreSelection(snapshot);
        refreshSelectionState();

        return new LauncherSelectionActionResult(null);
    }

    public static LauncherSelectionActionResult ClearSelection(
        IReadOnlyList<LauncherEntryViewModel> entries,
        Action refreshSelectionState,
        Func<bool> saveConfig)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(refreshSelectionState);
        ArgumentNullException.ThrowIfNull(saveConfig);

        var targets = entries.Where(entry => entry.IsSelected).ToList();
        if (targets.Count == 0)
            return new LauncherSelectionActionResult("当前没有已勾选项");

        var snapshot = targets
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
        foreach (var entry in targets)
        {
            entry.SetSelectionFromOwner(selected: false);
        }

        refreshSelectionState();
        if (saveConfig())
            return new LauncherSelectionActionResult($"已清空 {targets.Count} 个勾选项（已保存）");

        RestoreSelection(snapshot);
        refreshSelectionState();

        return new LauncherSelectionActionResult(null);
    }

    private static void RestoreSelection(IEnumerable<(LauncherEntryViewModel Entry, bool Selected)> snapshot)
    {
        foreach (var (entry, selected) in snapshot)
        {
            entry.SetSelectionFromOwner(selected);
        }
    }
}

public readonly record struct LauncherSelectionActionResult(string? StatusText);
