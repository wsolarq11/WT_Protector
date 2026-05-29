namespace WTLauncher.App.ViewModels;

public sealed class LauncherSelectionMutationSnapshot
{
    private readonly IReadOnlyList<SelectionState> _states;

    private LauncherSelectionMutationSnapshot(IReadOnlyList<SelectionState> states)
    {
        _states = states;
    }

    public static LauncherSelectionMutationSnapshot Capture(IEnumerable<LauncherEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return new LauncherSelectionMutationSnapshot(
            [.. entries.Select(entry => new SelectionState(entry, entry.IsSelected))]);
    }

    public static LauncherSelectionMutationSnapshot Capture(LauncherEntryViewModel entry, bool selected)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new LauncherSelectionMutationSnapshot([new SelectionState(entry, selected)]);
    }

    public void Restore()
    {
        foreach (var state in _states)
        {
            state.Entry.SetSelectionFromOwner(state.Selected);
        }
    }

    private readonly record struct SelectionState(LauncherEntryViewModel Entry, bool Selected);
}
