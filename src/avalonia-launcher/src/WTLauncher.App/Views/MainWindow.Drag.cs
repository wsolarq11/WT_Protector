using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow
{
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    private readonly MainWindowReorderAnimationScheduler _reorderAnimationScheduler = new();
    private string? _activeDragEntryId;

    private async void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel { IsCustomSortMode: true } viewModel)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var resolved = MainWindowEntryCardResolver.ResolveFromSource(sender);
        if (resolved is null)
            return;

        var (item, card) = resolved.Value;

        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(item.Id));
        e.Handled = true;
        _activeDragEntryId = item.Id;
        viewModel.BeginDragReorder(item.Id);
        ShowDragGhost(item, card, e);

        var effect = DragDropEffects.None;
        try
        {
            effect = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        }
        finally
        {
            HideDragGhost();
            FinishDragReorder(viewModel, effect);
            _activeDragEntryId = null;
        }
    }

    private void FinishDragReorder(MainWindowViewModel viewModel, DragDropEffects effect)
    {
        if (_activeDragEntryId is null)
            return;

        if (effect == DragDropEffects.Move)
            viewModel.CommitDragReorder();
        else
            viewModel.CancelDragReorder();
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
            MoveDragGhost(e);

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
        var insertAfter = MainWindowDragInsertDecision.ShouldInsertAfter(
            e.GetPosition(target.Card),
            target.Card.Bounds.Size,
            viewModel.IsDetailedListMode);
        var root = MainWindowEntriesRootSelection.Select(
            viewModel.IsDetailedListMode,
            viewModel.IsNamesViewMode,
            DetailsEntriesItemsControl,
            NamesEntriesItemsControl,
            CardsEntriesItemsControl);
        var before = MainWindowEntryCardBoundsCapture.Capture(root, this);
        if (viewModel.PreviewMoveEntry(draggedId, target.Entry.Id, insertAfter))
        {
            _reorderAnimationScheduler.Schedule(
                before,
                OnEntriesLayoutUpdated,
                handler => root.LayoutUpdated -= handler,
                handler => root.LayoutUpdated += handler);
        }
    }

    private (LauncherEntryViewModel Entry, Control Card)? ResolveEntryCardFromDragEvent(DragEventArgs e)
    {
        var direct = MainWindowEntryCardResolver.ResolveFromSource(e.Source);
        if (direct is not null)
            return direct;

        return MainWindowEntryCardResolver.ResolveFromSource(this.GetVisualAt(e.GetPosition(this)));
    }

    private void ShowDragGhost(
        LauncherEntryViewModel item,
        Control card,
        PointerPressedEventArgs e)
    {
        _overlayLayer ??= OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer is null || _dragGhost is not null)
            return;

        var pointerPosition = e.GetPosition(this);
        var ghost = MainWindowDragGhostFactory.Create(
            item.Name,
            item.Directory,
            item.TitleFontSize,
            card.Bounds.Width,
            card.Bounds.Height,
            pointerPosition.X,
            pointerPosition.Y);

        _dragGhost = ghost.Control;
        _overlayLayer.Children.Add(_dragGhost);
        SetDragGhostPosition(_dragGhost, ghost.Left, ghost.Top);
    }

    private void MoveDragGhost(DragEventArgs e)
    {
        if (_dragGhost is null)
            return;

        var pointerPosition = e.GetPosition(this);
        var ghostPosition = LauncherDragGhostLayoutCalculator.CalculatePosition(pointerPosition.X, pointerPosition.Y);
        SetDragGhostPosition(_dragGhost, ghostPosition.Left, ghostPosition.Top);
    }

    private static void SetDragGhostPosition(Border ghost, double left, double top)
    {
        Canvas.SetLeft(ghost, left);
        Canvas.SetTop(ghost, top);
    }

    private void HideDragGhost()
    {
        if (_overlayLayer is null || _dragGhost is null)
            return;

        _overlayLayer.Children.Remove(_dragGhost);
        _dragGhost = null;
    }

    private void OnEntriesLayoutUpdated(object? sender, EventArgs e)
    {
        var root = sender as Control;
        if (root is null)
            return;

        var before = _reorderAnimationScheduler.Consume(
            OnEntriesLayoutUpdated,
            handler => root.LayoutUpdated -= handler);
        if (before is null || before.Count == 0)
            return;

        var after = MainWindowEntryCardBoundsCapture.Capture(root, this);
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
}
