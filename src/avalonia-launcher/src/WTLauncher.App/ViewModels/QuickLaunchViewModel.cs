using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Lightweight quick-launch overlay state. It is an entry point only: filtering,
/// ranking, and launching all delegate to Core/entry owner behavior already used
/// by the main window.
/// </summary>
public sealed partial class QuickLaunchViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;

    public ObservableCollection<LauncherEntryViewModel> Results { get; } = [];

#pragma warning disable CA1822
    public string WindowTitle => LauncherLocalization.Format("quick.title");

    public string Placeholder => LauncherLocalization.Format("quick.placeholder");

    public string StaleBadge => LauncherLocalization.Format("quick.staleBadge");
#pragma warning restore CA1822

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private LauncherEntryViewModel? _selectedResult;

    public QuickLaunchViewModel(MainWindowViewModel owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        RefreshResults();
    }

    public void Reset()
    {
        Query = "";
        RefreshResults();
    }

    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            SelectedResult = null;
            return;
        }

        var current = SelectedResult is null ? -1 : Results.IndexOf(SelectedResult);
        var next = Math.Clamp(current + delta, 0, Results.Count - 1);
        SelectedResult = Results[next];
    }

    partial void OnSelectedResultChanged(LauncherEntryViewModel? value) => LaunchSelectedCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanLaunchSelected))]
    private void LaunchSelected()
    {
        var selected = SelectedResult;
        if (selected is null || !selected.LaunchCommand.CanExecute(null))
            return;

        selected.LaunchCommand.Execute(null);
    }

    partial void OnQueryChanged(string value) => RefreshResults();

    private bool CanLaunchSelected() => SelectedResult is not null;

    private void RefreshResults()
    {
        Results.Clear();
        foreach (var entry in _owner.Entries
            .Where(e => LauncherQuickMatch.IsMatch(Query, e.SnapshotEntry()))
            .OrderByDescending(e => LauncherQuickMatch.Score(Query, e.SnapshotEntry()))
            .ThenBy(e => e.Name)
            .Take(30))
        {
            Results.Add(entry);
        }

        SelectedResult = Results.FirstOrDefault();
    }
}
