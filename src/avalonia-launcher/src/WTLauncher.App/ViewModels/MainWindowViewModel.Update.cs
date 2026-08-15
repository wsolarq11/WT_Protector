using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

// Update-check command surface. Dialog orchestration lives in
// LauncherUpdateCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task CheckForUpdatesAsync() => _updateWorkflow.CheckForUpdatesAsync();
}
