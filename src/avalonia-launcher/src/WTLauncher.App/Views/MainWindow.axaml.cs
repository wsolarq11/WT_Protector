using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
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
    private Dictionary<string, Rect>? _pendingReorderAnimationSnapshot;

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
        viewModel.RequestNewPresetAsync = selectedEntryIds => LauncherDialogService.RequestNewPresetAsync(this, selectedEntryIds);
        viewModel.ConfirmDeletePresetAsync = ConfirmDeletePresetAsync;
        viewModel.RequestEntryDraftAsync = RequestEntryDraftAsync;
        viewModel.ConfirmDeleteEntryAsync = ConfirmDeleteEntryAsync;
        DragDrop.AddDragOverHandler(this, OnEntryDragOver);
        DragDrop.AddDropHandler(this, OnEntryDrop);
        SizeChanged += (_, _) => UpdateCardRegionSize(viewModel);
        Opened += (_, _) => UpdateCardRegionSize(viewModel);
        Closing += OnClosing;
    }

    private Task<bool> ConfirmLaunchAsync(LaunchEntry entry, LaunchPlan plan)
    {
        return LauncherDialogService.ConfirmLaunchAsync(this, entry, plan);
    }

    private Task<bool> ConfirmBatchLaunchAsync(IReadOnlyList<LaunchEntry> entries)
    {
        return LauncherDialogService.ConfirmBatchLaunchAsync(this, entries);
    }

    private Task<bool> ConfirmDeletePresetAsync(LauncherPresetViewModel preset)
    {
        return LauncherDialogService.ConfirmDeletePresetAsync(this, preset);
    }

    private Task<EntryDraft?> RequestEntryDraftAsync(LaunchEntry? entry)
    {
        return LauncherDialogService.RequestEntryDraftAsync(this, entry);
    }

    private Task<bool> ConfirmDeleteEntryAsync(LauncherEntryViewModel entry)
    {
        return LauncherDialogService.ConfirmDeleteEntryAsync(this, entry);
    }

    private void UpdateCardRegionSize(MainWindowViewModel viewModel)
    {
        // Header/footer take the stable chrome area; the view model owns the cheap grid sizing.
        viewModel.UpdateCardsAvailableSize(MainWindowLayoutMetrics.CalculateCardsAvailableSize(Bounds.Size));
    }

    private async void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel { IsCustomSortMode: true })
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var resolved = ResolveEntryCardFromSource(sender);
        if (resolved is null)
            return;

        var (item, card) = resolved.Value;

        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(item.Id));
        e.Handled = true;
        _activeDragEntryId = item.Id;
        var viewModel = (MainWindowViewModel)DataContext;
        viewModel.BeginDragReorder(item.Id);
        ShowDragGhost(item, card, e.GetPosition(this));
        var effect = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        HideDragGhost();
        if (_activeDragEntryId is not null)
        {
            if (effect == DragDropEffects.Move)
                viewModel.CommitDragReorder();
            else
                viewModel.CancelDragReorder();
        }
        _activeDragEntryId = null;
    }

    private void OnEntryDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = false;
            return;
        }

        if (_activeDragEntryId is not null)
            UpdateDragGhostPosition(e.GetPosition(this));

        var resolved = ResolveEntryCardFromDragEvent(e);
        var canDrop = viewModel.IsCustomSortMode
            && resolved is not null
            && e.DataTransfer.Formats.Contains(DataFormat.Text);
        e.DragEffects = canDrop
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = canDrop;

        if (!canDrop || _activeDragEntryId is null)
        {
            viewModel.ClearDragHover();
            return;
        }

        ApplyDragReorderPreview(e, viewModel, _activeDragEntryId, resolved!.Value);
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

        _activeDragEntryId = draggedId;
        ApplyDragReorderPreview(e, viewModel, draggedId, resolved.Value);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void ApplyDragReorderPreview(
        DragEventArgs e,
        MainWindowViewModel viewModel,
        string draggedId,
        (LauncherEntryViewModel Entry, Control Card) target)
    {
        var insertAfter = ShouldInsertAfter(e, viewModel, target.Card);
        var before = CaptureEntryCardBounds();
        if (viewModel.PreviewMoveEntry(draggedId, target.Entry.Id, insertAfter))
            ScheduleReorderAnimation(before);
    }

    private static bool ShouldInsertAfter(DragEventArgs e, MainWindowViewModel viewModel, Control card)
    {
        return MainWindowDragInsertDecision.ShouldInsertAfter(
            e.GetPosition(card),
            card.Bounds.Size,
            UsesVerticalInsertPreview(viewModel));
    }

    private (LauncherEntryViewModel Entry, Control Card)? ResolveEntryCardFromDragEvent(DragEventArgs e)
    {
        var direct = ResolveEntryCardFromSource(e.Source);
        if (direct is not null)
            return direct;

        var visual = this.GetVisualAt(e.GetPosition(this));
        return ResolveEntryCardFromSource(visual);
    }

    private static (LauncherEntryViewModel Entry, Control Card)? ResolveEntryCardFromSource(object? source)
    {
        if (source is StyledElement { DataContext: LauncherEntryViewModel directEntry } && source is Control directControl && directControl.Classes.Contains("card"))
            return (directEntry, directControl);

        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is Control { DataContext: LauncherEntryViewModel entry } control && control.Classes.Contains("card"))
                return (entry, control);
        }

        return null;
    }

    private void ShowDragGhost(LauncherEntryViewModel item, Control sourceCard, Point position)
    {
        _overlayLayer ??= OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer is null || _dragGhost is not null)
            return;

        var layout = LauncherDragGhostLayoutCalculator.Calculate(
            sourceCard.Bounds.Width,
            sourceCard.Bounds.Height,
            position.X,
            position.Y);

        var ghost = new Border
        {
            Width = layout.Width,
            Height = layout.Height,
            Classes = { "card" },
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#38BDF8"),
            BorderThickness = new Thickness(2),
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
        SetDragGhostPosition(layout.Left, layout.Top);
    }

    private void UpdateDragGhostPosition(Point position)
    {
        var ghostPosition = LauncherDragGhostLayoutCalculator.CalculatePosition(position.X, position.Y);
        SetDragGhostPosition(ghostPosition.Left, ghostPosition.Top);
    }

    private void SetDragGhostPosition(double left, double top)
    {
        if (_dragGhost is null)
            return;

        Canvas.SetLeft(_dragGhost, left);
        Canvas.SetTop(_dragGhost, top);
    }

    private void HideDragGhost()
    {
        if (_overlayLayer is null || _dragGhost is null)
            return;

        _overlayLayer.Children.Remove(_dragGhost);
        _dragGhost = null;
    }

    private static bool UsesVerticalInsertPreview(MainWindowViewModel viewModel)
    {
        return viewModel.IsDetailedListMode;
    }

    private Control? GetActiveEntriesRoot()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return null;

        if (viewModel.IsDetailedListMode)
            return DetailsEntriesItemsControl;

        if (viewModel.IsNamesViewMode)
            return NamesEntriesItemsControl;

        return CardsEntriesItemsControl;
    }

    private Dictionary<string, Rect> CaptureEntryCardBounds()
    {
        var bounds = new Dictionary<string, Rect>(StringComparer.Ordinal);
        var root = GetActiveEntriesRoot();
        if (root is null)
            return bounds;

        foreach (var card in root.GetVisualDescendants().OfType<Border>())
        {
            if (!card.Classes.Contains("card"))
                continue;
            if (card.DataContext is not LauncherEntryViewModel entry)
                continue;

            var origin = card.TranslatePoint(new Point(0, 0), this);
            if (origin is null)
                continue;

            bounds[entry.Id] = new Rect(origin.Value, card.Bounds.Size);
        }

        return bounds;
    }

    private void ScheduleReorderAnimation(Dictionary<string, Rect> before)
    {
        var root = GetActiveEntriesRoot();
        if (before.Count == 0 || root is null)
            return;

        _pendingReorderAnimationSnapshot = before;
        root.LayoutUpdated -= OnEntriesLayoutUpdated;
        root.LayoutUpdated += OnEntriesLayoutUpdated;
    }

    private void OnEntriesLayoutUpdated(object? sender, EventArgs e)
    {
        var root = sender as Control;
        if (root is null)
            return;

        root.LayoutUpdated -= OnEntriesLayoutUpdated;

        var before = _pendingReorderAnimationSnapshot;
        _pendingReorderAnimationSnapshot = null;
        if (before is null || before.Count == 0)
            return;

        var after = CaptureEntryCardBounds();
        if (after.Count == 0)
            return;

        foreach (var card in root.GetVisualDescendants().OfType<Border>())
        {
            if (!card.Classes.Contains("card"))
                continue;
            if (card.DataContext is not LauncherEntryViewModel entry)
                continue;
            if (!before.TryGetValue(entry.Id, out var oldRect))
                continue;
            if (!after.TryGetValue(entry.Id, out var newRect))
                continue;

            var deltaX = oldRect.X - newRect.X;
            var deltaY = oldRect.Y - newRect.Y;
            if (Math.Abs(deltaX) < 0.5 && Math.Abs(deltaY) < 0.5)
                continue;

            // FLIP: jump back without transition, then let the style transition animate to the final position.
            card.Transitions = new Transitions();
            card.RenderTransform = TransformOperations.Parse(FormattableString.Invariant($"translate({deltaX:0.##}px,{deltaY:0.##}px)"));

            Dispatcher.UIThread.Post(() =>
            {
                if (card.IsEffectivelyVisible is false)
                    return;
                card.ClearValue(Animatable.TransitionsProperty);
                card.RenderTransform = TransformOperations.Parse("translate(0px,0px)");
            }, DispatcherPriority.Render);
        }
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

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: LauncherEntryViewModel entry })
            return;

        if (e.Source is Control source && MainWindowEntryRowInteraction.IsInteractiveSource(source))
            return;

        if (entry.LaunchCommand.CanExecute(null))
            entry.LaunchCommand.Execute(null);

        e.Handled = true;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        MainWindowClosingWorkflow.TrySaveWindowBounds(
            _settings,
            _settingsPath,
            Width,
            Height,
            status =>
            {
                if (DataContext is MainWindowViewModel viewModel)
                    viewModel.StatusText = status;
            });
    }
}
