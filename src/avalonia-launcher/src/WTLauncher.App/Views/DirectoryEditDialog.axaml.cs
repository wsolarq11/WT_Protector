using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class DirectoryEditDialog : Window
{
    public DirectoryEditDialog()
    {
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public DirectoryEditDialog(string title, string label, string initialValue, int affectedEntryCount)
    {
        InitializeComponent();
        ApplyLocalizedChrome();

        Title = title;
        TitleText.Text = title;
        LabelText.Text = label;
        Input.Text = initialValue;

        if (affectedEntryCount > 0)
        {
            AffectedHint.Text = LauncherLocalization.Format("directoryEdit.affectedHint", affectedEntryCount);
            AffectedHint.IsVisible = true;
        }

        Opened += (_, _) => Input.Focus();
    }

    private void ApplyLocalizedChrome()
    {
        Input.Watermark = LauncherLocalization.Format("directoryEdit.placeholder");
        BrowseButton.Content = LauncherLocalization.Format("directoryEdit.browse");
        AutomationProperties.SetName(BrowseButton, LauncherLocalization.Format("directoryEdit.browseAutomation"));
        CancelButton.Content = LauncherLocalization.Format("directoryEdit.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("directoryEdit.cancelAutomation"));
        OkButton.Content = LauncherLocalization.Format("directoryEdit.ok");
        AutomationProperties.SetName(OkButton, LauncherLocalization.Format("directoryEdit.okAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var startDir = !string.IsNullOrEmpty(Input.Text)
                ? await StorageProvider.TryGetFolderFromPathAsync(Input.Text)
                : null;
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LauncherLocalization.Format("directoryEdit.pickerTitle"),
                AllowMultiple = false,
                SuggestedStartLocation = startDir,
            });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } localPath)
                Input.Text = localPath;
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryEdit.Browse");
        }
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        var v = Input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(v))
        {
            ErrorText.Text = LauncherLocalization.Format("directoryEdit.required");
            ErrorText.IsVisible = true;
            return;
        }
        Close(v);
    }
}
