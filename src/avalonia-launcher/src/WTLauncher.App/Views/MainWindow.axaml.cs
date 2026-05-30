using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    private const double CardsRegionHorizontalChromeWidth = 48;
    private const double CardsRegionVerticalChromeHeight = 270;
    private string? _settingsPath;
    private LauncherSettings? _settings;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, string settingsPath, LauncherSettings settings)
        : this()
    {
        DataContext = viewModel;
        _settingsPath = settingsPath;
        _settings = settings;
        viewModel.ConfirmLaunchAsync = (entry, plan) => new ConfirmLaunchWindow(entry, plan).ShowDialog<bool>(this);
        viewModel.ConfirmBatchLaunchAsync = entries => new ConfirmBatchLaunchWindow(entries).ShowDialog<bool>(this);
        viewModel.RequestNewPresetAsync = selectedEntryIds =>
            new PresetDetailsWindow(selectedEntryIds).ShowDialog<string?>(this);
        viewModel.ConfirmDeletePresetAsync = preset => new ConfirmPresetDeleteWindow(preset).ShowDialog<bool>(this);
        viewModel.RequestEntryDraftAsync = entry => new EntryDetailsWindow(entry).ShowDialog<EntryDraft?>(this);
        viewModel.ConfirmDeleteEntryAsync = entry => new ConfirmEntryDeleteWindow(entry).ShowDialog<bool>(this);
        DragDrop.AddDragOverHandler(this, OnEntryDragOver);
        DragDrop.AddDropHandler(this, OnEntryDrop);
        SizeChanged += (_, _) => UpdateCardRegionSize(viewModel);
        Opened += (_, _) => UpdateCardRegionSize(viewModel);
        Closing += OnClosing;
    }

    private void UpdateCardRegionSize(MainWindowViewModel viewModel)
    {
        // Header/footer take the stable chrome area; the view model owns the cheap grid sizing.
        viewModel.UpdateCardsAvailableSize(new Size(
            Bounds.Width - CardsRegionHorizontalChromeWidth,
            Bounds.Height - CardsRegionVerticalChromeHeight));
    }

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (TryOpenEntryCardContextMenu(control))
            e.Handled = true;
    }

    private static bool TryOpenEntryCardContextMenu(Control source)
    {
        var resolved = MainWindowEntryCardContextMenu.ResolveFromSource(source);
        if (resolved is null)
            return false;

        resolved.Value.Menu.Open(resolved.Value.Card);
        return true;
    }

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: LauncherEntryViewModel entry })
            return;

        if (!MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(e.Source))
            return;

        if (entry.LaunchCommand.CanExecute(null))
            entry.LaunchCommand.Execute(null);

        e.Handled = true;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_settings is null || _settingsPath is null)
            return;

        if (LauncherSettingsStore.TrySaveWindowBounds(_settings, _settingsPath, Width, Height))
            return;

        if (DataContext is MainWindowViewModel viewModel)
            viewModel.StatusText = "窗口尺寸保存失败；下次将使用上次保存的尺寸";
    }
}
