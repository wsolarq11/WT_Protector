using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

// Import/export/migration command surface. Command bodies and store
// transactions live in LauncherExchangeCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task ExportAllConfigAsync() => _exchangeWorkflow.ExportAllAsync();

    [RelayCommand]
    private Task ExportSelectedConfigAsync() => _exchangeWorkflow.ExportSelectedAsync();

    [RelayCommand]
    private Task ImportConfigAsync() => _exchangeWorkflow.ImportAsync();
}
