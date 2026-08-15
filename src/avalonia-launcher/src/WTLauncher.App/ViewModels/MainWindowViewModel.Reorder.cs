namespace WTLauncher.App.ViewModels;

// Keyboard-accessible Move Up / Move Down command surface. Command bodies live
// in LauncherReorderCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    public bool CanMoveEntryUp(LauncherEntryViewModel entry) =>
        _reorderWorkflow.CanMoveEntryUp(entry);

    public bool CanMoveEntryDown(LauncherEntryViewModel entry) =>
        _reorderWorkflow.CanMoveEntryDown(entry);

    public void MoveEntryUp(LauncherEntryViewModel entry) =>
        _reorderWorkflow.MoveEntryUp(entry);

    public void MoveEntryDown(LauncherEntryViewModel entry) =>
        _reorderWorkflow.MoveEntryDown(entry);

    private void RefreshReorderCommandState() =>
        _reorderWorkflow.RefreshReorderCommandState();
}
