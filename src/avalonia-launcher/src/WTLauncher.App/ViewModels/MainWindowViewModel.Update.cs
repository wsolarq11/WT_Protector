using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public static string CurrentVersion => AppInfo.Version;

    [RelayCommand]
    private Task CheckForUpdatesAsync() =>
        _dialogService.OpenUpdateCheckAsync(CurrentVersion, CurrentSettings.UpdateManifestUrl);
}
