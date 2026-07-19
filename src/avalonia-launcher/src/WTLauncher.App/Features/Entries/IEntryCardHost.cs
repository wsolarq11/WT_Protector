namespace WTLauncher.App.Features.Entries;

/// <summary>
/// Contract between <see cref="ViewModels.LauncherEntryViewModel"/> and its owning
/// ViewModel. Extracted to break the direct <c>MainWindowViewModel</c> dependency,
/// enabling cleaner entry-list extraction and testability.
/// </summary>
public interface IEntryCardHost
{
    // Layout presentation — consumed by LauncherEntryViewModel.Layout.cs
    public bool IsDetailedListMode { get; }
    public bool IsNamesViewMode { get; }

    // Entry card commands — routing to the owning ViewModel
    public void UpdateSelection(ViewModels.LauncherEntryViewModel entry, bool selected);
    public void ShowDryRun(ViewModels.LauncherEntryViewModel entry);
    public Task LaunchAsync(ViewModels.LauncherEntryViewModel entry);
    public Task EditEntryAsync(ViewModels.LauncherEntryViewModel entry);
    public Task DeleteEntryAsync(ViewModels.LauncherEntryViewModel entry);
    public Task CloneEntryAsync(ViewModels.LauncherEntryViewModel entry);
    public void MoveEntryUp(ViewModels.LauncherEntryViewModel entry);
    public bool CanMoveEntryUp(ViewModels.LauncherEntryViewModel entry);
    public void MoveEntryDown(ViewModels.LauncherEntryViewModel entry);
    public bool CanMoveEntryDown(ViewModels.LauncherEntryViewModel entry);
}
