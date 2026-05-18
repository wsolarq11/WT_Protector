using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
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
        viewModel.RequestEntryDraftAsync = RequestEntryDraftAsync;
        viewModel.ConfirmDeleteEntryAsync = ConfirmDeleteEntryAsync;
        DragDrop.AddDragOverHandler(this, OnEntryDragOver);
        DragDrop.AddDropHandler(this, OnEntryDrop);
        SizeChanged += (_, _) => UpdateCardRegionSize(viewModel);
        Opened += (_, _) => UpdateCardRegionSize(viewModel);
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

    private async Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? entry)
    {
        var dialog = new EntryDetailsWindow(entry);
        return await dialog.ShowDialog<EntryDraft?>(this).ConfigureAwait(true);
    }

    private async Task<bool> ConfirmDeleteEntryAsync(LauncherEntryViewModel entry)
    {
        var dialog = new ConfirmEntryDeleteWindow(entry);
        return await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
    }

    private void UpdateCardRegionSize(MainWindowViewModel viewModel)
    {
        // Header/footer take the stable chrome area; the view model owns the cheap grid sizing.
        viewModel.UpdateCardsAvailableSize(Math.Max(220, Bounds.Width - 48), Math.Max(140, Bounds.Height - 270));
    }

    private async void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel { IsCustomSortMode: true } || sender is not Control { DataContext: LauncherEntryViewModel item })
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(item.Id));
        e.Handled = true;
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
    }

    private void OnEntryDragOver(object? sender, DragEventArgs e)
    {
        var canDrop = DataContext is MainWindowViewModel { IsCustomSortMode: true }
            && ResolveEntryFromDragEvent(e) is not null
            && e.DataTransfer.Formats.Contains(DataFormat.Text);
        e.DragEffects = canDrop
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = canDrop;
    }

    private void OnEntryDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var target = ResolveEntryFromDragEvent(e);
        if (target is null)
            return;
        if (!e.DataTransfer.Formats.Contains(DataFormat.Text))
            return;

        var draggedId = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(draggedId))
            return;

        viewModel.MoveEntryBefore(draggedId, target.Id);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private LauncherEntryViewModel? ResolveEntryFromDragEvent(DragEventArgs e)
    {
        var direct = ResolveEntryFromSource(e.Source);
        if (direct is not null)
            return direct;

        var visual = this.GetVisualAt(e.GetPosition(this));
        return ResolveEntryFromSource(visual);
    }

    private static LauncherEntryViewModel? ResolveEntryFromSource(object? source)
    {
        if (source is StyledElement { DataContext: LauncherEntryViewModel directEntry })
            return directEntry;

        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is StyledElement { DataContext: LauncherEntryViewModel entry })
                return entry;
        }

        return null;
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
