using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class PresetDetailsWindow : Window
{
    private readonly IReadOnlySet<string> _existingPresetNames = null!;
    private readonly IEntryDraftValidator _validator = null!;

    public PresetDetailsWindow()
    {
        _validator = new EntryDraftValidator();
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public PresetDetailsWindow(
        LauncherPresetSaveTarget target,
        IReadOnlySet<string> existingPresetNames,
        IEntryDraftValidator? validator = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(existingPresetNames);

        _existingPresetNames = existingPresetNames;
        _validator = validator ?? new EntryDraftValidator();

        InitializeComponent();
        ApplyLocalizedChrome();

        SummaryText.Text = LauncherLocalization.Format("presetDetails.summary", target.EntryCount);

        Opened += (_, _) =>
        {
            NameBox.Focus();
            Dispatcher.UIThread.Post(() => NameBox.SelectAll(), DispatcherPriority.Loaded);
        };
    }

    private void ApplyLocalizedChrome()
    {
        Title = LauncherLocalization.Format("presetDetails.title");
        HeadingText.Text = LauncherLocalization.Format("presetDetails.heading");
        NameLabel.Text = LauncherLocalization.Format("presetDetails.name");
        CancelButton.Content = LauncherLocalization.Format("presetDetails.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("presetDetails.cancelAutomation"));
        SaveButton.Content = LauncherLocalization.Format("presetDetails.save");
        AutomationProperties.SetName(SaveButton, LauncherLocalization.Format("presetDetails.saveAutomation"));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close((string?)null);

    private void OnSaveClicked(object? sender, RoutedEventArgs e) => Submit();

    // ponytail: intentional duplicate of ViewModel validation.
    // This UI-layer check gives instant visual feedback (red error text + focus)
    // before the save-path delegates to ViewModel validation. Keep both.
    private void Submit()
    {
        var name = _validator.NormalizeText(NameBox.Text);
        if (name.Length == 0)
        {
            ErrorText.Text = LauncherLocalization.Format("presetDetails.nameRequired");
            ErrorText.IsVisible = true;
            NameBox.Focus();
            return;
        }

        if (_existingPresetNames.Contains(name))
        {
            ErrorText.Text = LauncherLocalization.Format("presetDetails.nameExists");
            ErrorText.IsVisible = true;
            NameBox.Focus();
            return;
        }

        Close(name);
    }
}
