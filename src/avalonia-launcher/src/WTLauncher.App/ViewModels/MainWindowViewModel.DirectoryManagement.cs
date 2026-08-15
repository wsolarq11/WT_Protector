using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Directory management dialog surface. Command bodies and edit transactions
// live in LauncherMaintenanceCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task OpenDirectoryManagementAsync() =>
        _maintenanceWorkflow.OpenDirectoryManagementAsync();

    DirectoryManagementResult IDirectoryEditApplier.Apply(DirectoryManagementEdit edit) =>
        _maintenanceWorkflow.ApplyDirectoryManagementEdit(edit);

    internal DirectoryManagementResult ApplyDirectoryManagementEdit(DirectoryManagementEdit edit) =>
        _maintenanceWorkflow.ApplyDirectoryManagementEdit(edit);
}
