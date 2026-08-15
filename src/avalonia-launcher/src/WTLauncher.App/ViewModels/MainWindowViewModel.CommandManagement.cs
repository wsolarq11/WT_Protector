using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Command/template management dialog surface. Command bodies and edit
// transactions live in LauncherMaintenanceCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task OpenCommandManagementAsync() =>
        _maintenanceWorkflow.OpenCommandManagementAsync();

    CommandManagementResult ICommandEditApplier.Apply(CommandManagementEdit edit) =>
        _maintenanceWorkflow.ApplyCommandManagementEdit(edit);

    internal CommandManagementResult ApplyCommandManagementEdit(CommandManagementEdit edit) =>
        _maintenanceWorkflow.ApplyCommandManagementEdit(edit);
}
