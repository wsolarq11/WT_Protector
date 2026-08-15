using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;

namespace WTLauncher.App.ViewModels;

/// <summary>Update-check command workflow.</summary>
public sealed partial class LauncherUpdateCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly IDialogService _dialogService;

    public LauncherUpdateCommandWorkflow(
        ICommandExecutionContext context,
        IDialogService dialogService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    public Task CheckForUpdatesAsync() =>
        _dialogService.OpenUpdateCheckAsync(
            AppInfo.Version,
            _context.Store.Select(s => s.Settings.UpdateManifestUrl));
}
