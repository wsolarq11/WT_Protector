using Avalonia;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task OpenAccentColorAsync()
    {
        var result = await _dialogService.OpenAccentColorAsync(
            CurrentSettings.AccentColor, CurrentSettings.AccentPresets);
        if (result is null) return;
        if (TryPersistSettingsChange(
                s => s with { AccentColor = result.AccentColor, AccentPresets = result.Presets },
                LauncherSettingsStatus.AccentColorSaved(result.AccentColor)))
            App.ApplyAccentRamp(Application.Current!.Resources, result.AccentColor);
    }
}
