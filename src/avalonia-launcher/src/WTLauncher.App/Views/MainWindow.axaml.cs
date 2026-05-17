using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    private string? _settingsPath;
    private LauncherSettings? _settings;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Attach(MainWindowViewModel viewModel, string settingsPath, LauncherSettings settings)
    {
        _settingsPath = settingsPath;
        _settings = settings;
        viewModel.ConfirmLaunchAsync = ConfirmLaunchAsync;
        viewModel.ConfirmBatchLaunchAsync = ConfirmBatchLaunchAsync;
        viewModel.RequestNewPresetAsync = RequestNewPresetAsync;
        viewModel.ConfirmDeletePresetAsync = ConfirmDeletePresetAsync;
        Closing += OnClosing;
    }

    private async Task<bool> ConfirmLaunchAsync(LaunchEntry entry, LaunchPlan plan)
    {
        var dialog = new ConfirmLaunchWindow(entry, plan);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }

    private async Task<bool> ConfirmBatchLaunchAsync(IReadOnlyList<LaunchEntry> entries)
    {
        var dialog = new ConfirmBatchLaunchWindow(entries);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }

    private async Task<PresetDraft?> RequestNewPresetAsync()
    {
        var dialog = new PresetDetailsWindow(GetSelectedEntryIds());
        return await dialog.ShowDialog<PresetDraft?>(this).ConfigureAwait(true);
    }

    private async Task<bool> ConfirmDeletePresetAsync(LauncherPresetViewModel preset)
    {
        var dialog = new ConfirmPresetDeleteWindow(preset);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }

    private IReadOnlyList<string> GetSelectedEntryIds()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return [];

        return viewModel.Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToList();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_settings is null || _settingsPath is null)
            return;

        _settings.Window.Width = Width;
        _settings.Window.Height = Height;
        LauncherSettingsStore.Save(_settings, _settingsPath);
    }
}
