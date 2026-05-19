using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    private string? _settingsPath;
    private LauncherSettings? _settings;
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    private string? _activeDragEntryId;
    private bool _dragOperationCommitted;

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
        DragDrop.AddDragEnterHandler(this, OnEntryDragEnter);
        DragDrop.AddDragOverHandler(this, OnEntryDragOver);
        DragDrop.AddDragLeaveHandler(this, OnEntryDragLeave);
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
        _activeDragEntryId = item.Id;
        _dragOperationCommitted = false;
        ((MainWindowViewModel)DataContext).BeginDragReorder(item.Id);
        ShowDragGhost(item, e.GetPosition(this));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        HideDragGhost();
        if (!_dragOperationCommitted && _activeDragEntryId is not null && DataContext is MainWindowViewModel viewModel)
            viewModel.CancelDragReorder();
        _activeDragEntryId = null;
        _dragOperationCommitted = false;
    }

    private void OnEntryDragEnter(object? sender, DragEventArgs e)
    {
        if (_activeDragEntryId is null || DataContext is not MainWindowViewModel { IsCustomSortMode: true })
            return;

        if (!e.DataTransfer.Formats.Contains(DataFormat.Text))
            return;

        if (_dragGhost is not null)
            return;

        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var sourceItem = viewModel.Entries.FirstOrDefault(entry => string.Equals(entry.Id, _activeDragEntryId, StringComparison.Ordinal));
        if (sourceItem is not null)
            ShowDragGhost(sourceItem, e.GetPosition(this));
    }

    private void OnEntryDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = false;
            return;
        }

        var resolved = ResolveEntryCardFromDragEvent(e);
        var canDrop = viewModel.IsCustomSortMode
            && resolved is not null
            && e.DataTransfer.Formats.Contains(DataFormat.Text);
        e.DragEffects = canDrop
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = canDrop;

        if (!canDrop || _activeDragEntryId is null)
            return;

        UpdateDragGhostPosition(e.GetPosition(this));
        var (target, card) = resolved!.Value;
        var positionInCard = e.GetPosition(card);
        var insertAfter = positionInCard.X >= (card.Bounds.Width * 0.5);
        viewModel.PreviewMoveEntry(_activeDragEntryId, target.Id, insertAfter);
    }

    private void OnEntryDragLeave(object? sender, DragEventArgs e)
    {
        if (_activeDragEntryId is null)
            return;

        HideDragGhost();
    }

    private void OnEntryDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var resolved = ResolveEntryCardFromDragEvent(e);
        if (resolved is null)
            return;
        if (!e.DataTransfer.Formats.Contains(DataFormat.Text))
            return;

        var draggedId = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(draggedId))
            return;

        var (target, card) = resolved.Value;
        var positionInCard = e.GetPosition(card);
        var insertAfter = positionInCard.X >= (card.Bounds.Width * 0.5);
        _activeDragEntryId = draggedId;
        viewModel.PreviewMoveEntry(draggedId, target.Id, insertAfter);
        _dragOperationCommitted = true;
        viewModel.CommitDragReorder();
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private (LauncherEntryViewModel Entry, Border Card)? ResolveEntryCardFromDragEvent(DragEventArgs e)
    {
        var direct = ResolveEntryCardFromSource(e.Source);
        if (direct is not null)
            return direct;

        var visual = this.GetVisualAt(e.GetPosition(this));
        return ResolveEntryCardFromSource(visual);
    }

    private static (LauncherEntryViewModel Entry, Border Card)? ResolveEntryCardFromSource(object? source)
    {
        if (source is StyledElement { DataContext: LauncherEntryViewModel directEntry } && source is Border directBorder && directBorder.Classes.Contains("card"))
            return (directEntry, directBorder);

        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is Border { DataContext: LauncherEntryViewModel entry } border && border.Classes.Contains("card"))
                return (entry, border);
        }

        return null;
    }

    private void ShowDragGhost(LauncherEntryViewModel item, Point position)
    {
        _overlayLayer ??= OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer is null || _dragGhost is not null)
            return;

        var ghost = new Border
        {
            Width = item.CardWidth,
            Height = item.CardHeight,
            Background = Brushes.White,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13),
            Opacity = 0.86,
            IsHitTestVisible = false,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 10,
                Blur = 28,
                Spread = 0,
                Color = Color.Parse("#220F172A"),
            }),
            Child = new StackPanel
            {
                Margin = new Thickness(13),
                Spacing = 8,
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = item.Name,
                        FontSize = item.TitleFontSize,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Brush.Parse("#0F172A"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                    new TextBlock
                    {
                        Text = item.Directory,
                        FontFamily = FontFamily.Parse("Cascadia Code, Consolas"),
                        FontSize = 12,
                        Foreground = Brush.Parse("#64748B"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                },
            },
        };

        _dragGhost = ghost;
        _overlayLayer.Children.Add(ghost);
        UpdateDragGhostPosition(position);
    }

    private void UpdateDragGhostPosition(Point position)
    {
        if (_dragGhost is null)
            return;

        const double offsetX = 16;
        const double offsetY = 16;
        Canvas.SetLeft(_dragGhost, position.X + offsetX);
        Canvas.SetTop(_dragGhost, position.Y + offsetY);
    }

    private void HideDragGhost()
    {
        if (_overlayLayer is null || _dragGhost is null)
            return;

        _overlayLayer.Children.Remove(_dragGhost);
        _dragGhost = null;
    }

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        Border? card = null;
        foreach (var ancestor in control.GetSelfAndVisualAncestors())
        {
            if (ancestor is Border border && border.Classes.Contains("card"))
            {
                card = border;
                break;
            }
        }

        if (card?.ContextMenu is not ContextMenu menu)
            return;

        menu.Open(card);
        e.Handled = true;
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
