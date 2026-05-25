using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
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

        var resolved = MainWindowEntryCardResolver.ResolveFromSource(sender);
        if (resolved is null)
            return;

        var (item, card) = resolved.Value;

        var data = MainWindowDragTextTransfer.CreateEntryIdTransfer(item.Id);
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
        var decision = MainWindowDragDropDecision.EvaluateDragOver(
            viewModel.IsCustomSortMode,
            resolved is not null,
            e.DataTransfer,
            _activeDragEntryId);
        e.DragEffects = decision.Effects;
        e.Handled = decision.Handled;

        if (decision.ShouldClearHover)
        {
            viewModel.ClearDragHover();
            return;
        }

        ApplyDragReorderPreview(e, viewModel, decision.PreviewDraggedEntryId!, resolved!.Value);
    }

    private void OnEntryDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var resolved = ResolveEntryCardFromDragEvent(e);
        var decision = MainWindowDragDropDecision.EvaluateDrop(resolved is not null, e.DataTransfer);
        if (!decision.Handled)
            return;

        _activeDragEntryId = decision.DraggedEntryId;
        ApplyDragReorderPreview(e, viewModel, decision.DraggedEntryId!, resolved!.Value);
        e.DragEffects = decision.Effects;
        e.Handled = decision.Handled;
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
        var direct = MainWindowEntryCardResolver.ResolveFromSource(e.Source);
        if (direct is not null)
            return direct;

        var visual = this.GetVisualAt(e.GetPosition(this));
        return MainWindowEntryCardResolver.ResolveFromSource(visual);
    }

    private void ShowDragGhost(LauncherEntryViewModel item, Control sourceCard, Point position)
    {
        _overlayLayer ??= OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer is null || _dragGhost is not null)
            return;

        var ghost = MainWindowDragGhostFactory.Create(
            item,
            sourceCard.Bounds.Width,
            sourceCard.Bounds.Height,
            position.X,
            position.Y);

        _dragGhost = ghost.Control;
        _overlayLayer.Children.Add(ghost.Control);
        SetDragGhostPosition(ghost.Left, ghost.Top);
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
        var root = GetActiveEntriesRoot();
        if (root is null)
            return [];

        return MainWindowEntryCardBoundsCapture.Capture(root, this);
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

        foreach (var target in MainWindowReorderFlipAnimation.FindTargets(root, before, after))
        {
            // FLIP: jump back without transition, then let the style transition animate to the final position.
            MainWindowReorderFlipAnimation.ApplyStart(target.Card, target.Offset);

            Dispatcher.UIThread.Post(() =>
            {
                MainWindowReorderFlipAnimation.TryApplyFinish(target.Card, target.Card.IsEffectivelyVisible);
            }, DispatcherPriority.Render);
        }
    }

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (MainWindowCardContextMenu.TryOpenFor(control))
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
