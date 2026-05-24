using CommunityToolkit.Mvvm.Input;

namespace WTLauncher.App.ViewModels;

public static class LauncherCommandNotifications
{
    public static void NotifySelection(
        IRelayCommand launchSelected,
        IRelayCommand clearSelection,
        IRelayCommand applyPreset,
        IRelayCommand saveSelectionAsPreset,
        IRelayCommand updateSelectedPreset,
        IRelayCommand deleteSelectedPreset)
    {
        ArgumentNullException.ThrowIfNull(launchSelected);
        ArgumentNullException.ThrowIfNull(clearSelection);
        ArgumentNullException.ThrowIfNull(applyPreset);

        launchSelected.NotifyCanExecuteChanged();
        clearSelection.NotifyCanExecuteChanged();
        applyPreset.NotifyCanExecuteChanged();
        NotifyPresetManagement(
            saveSelectionAsPreset,
            updateSelectedPreset,
            deleteSelectedPreset);
    }

    public static void NotifyPresetManagement(
        IRelayCommand saveSelectionAsPreset,
        IRelayCommand updateSelectedPreset,
        IRelayCommand deleteSelectedPreset)
    {
        ArgumentNullException.ThrowIfNull(saveSelectionAsPreset);
        ArgumentNullException.ThrowIfNull(updateSelectedPreset);
        ArgumentNullException.ThrowIfNull(deleteSelectedPreset);

        saveSelectionAsPreset.NotifyCanExecuteChanged();
        updateSelectedPreset.NotifyCanExecuteChanged();
        deleteSelectedPreset.NotifyCanExecuteChanged();
    }
}
