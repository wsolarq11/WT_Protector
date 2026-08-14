using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task OpenRunHistoryAsync() => _dialogService.OpenRunHistoryAsync(_runHistory);

    private void AppendRunHistory(IEnumerable<LauncherRunHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _runHistory = LauncherRunHistory.AppendRange(
            _runHistory,
            entries,
            LauncherRunHistory.DefaultMaxEntries);
        OnPropertyChanged(nameof(RunHistoryCount));
    }
}
