namespace WTLauncher.App.ViewModels;

internal static class LauncherSelectionPersistence
{
    public static bool UpdateSingle(
        LauncherEntryViewModel item,
        bool selected,
        Action refreshSelectionState,
        Func<bool> saveConfig)
    {
        var previousSelected = item.Entry.Selected;
        item.Entry.Selected = selected;
        refreshSelectionState();
        if (saveConfig())
            return true;

        item.SetSelectionFromOwner(previousSelected);
        refreshSelectionState();
        return false;
    }

    public static bool SetSelection(
        IReadOnlyList<LauncherEntryViewModel> entries,
        bool selected,
        Action refreshSelectionState,
        Func<bool> saveConfig)
    {
        var snapshot = entries
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selected);
        }

        refreshSelectionState();
        if (saveConfig())
            return true;

        RestoreSelection(snapshot);
        refreshSelectionState();
        return false;
    }

    public static bool ReplaceSelection(
        IReadOnlyList<LauncherEntryViewModel> entries,
        IReadOnlySet<string> selectedIds,
        Action refreshSelectionState,
        Func<bool> saveConfig)
    {
        var snapshot = entries
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Entry.Id));
        }

        refreshSelectionState();
        if (saveConfig())
            return true;

        RestoreSelection(snapshot);
        refreshSelectionState();
        return false;
    }

    private static void RestoreSelection(IEnumerable<(LauncherEntryViewModel Entry, bool Selected)> snapshot)
    {
        foreach (var (entry, selected) in snapshot)
        {
            entry.SetSelectionFromOwner(selected);
        }
    }
}
