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
    private Dictionary<string, (LauncherEntryViewModel Entry, Control Card)>? _dragCardCache;

    // P2: Suppress XAML transitions during active drag to avoid layout thrashing.
    private const string SuppressTransitionsClass = "suppress-drag-transitions";

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
        MainWindowDragEntryTransfer.SetEntryId(data, item.Id);
        e.Handled = true;
        _activeDragEntryId = item.Id;
        viewModel.BeginDragReorder(item.Id);
        _dragCardCache = BuildDragCardCache();
        ShowDragGhost(item, card, e);

        // P2: Suppress 0.12s XAML transitions on all card entries during drag.
        SuppressDragTransitions(true);

        var effect = DragDropEffects.None;
        try
        {
            effect = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        }
        finally
        {
            SuppressDragTransitions(false);
            HideDragGhost();
            FinishDragReorder(viewModel, effect);
            _activeDragEntryId = null;
        }
    }

    private void FinishDragReorder(MainWindowViewModel viewModel, DragDropEffects effect)
    {
        _dragCardCache = null;

        if (_activeDragEntryId is null)
            return;

        if (effect == DragDropEffects.Move)
            viewModel.CommitDragReorder();
        else
            viewModel.CancelDragReorder();
    }

    /// <summary>
    /// P2: Add/remove CSS class on all ItemsControl hosts to disable
    /// the 0.12s transitions on Border.card elements during drag.
    /// </summary>
    private void SuppressDragTransitions(bool suppress)
    {
        var itemsControls = new[] { CardsEntriesItemsControl, DetailsEntriesItemsControl, NamesEntriesItemsControl };
        foreach (var ic in itemsControls)
        {
            if (ic is null) continue;
            if (suppress)
                ic.Classes.Add(SuppressTransitionsClass);
            else
                ic.Classes.Remove(SuppressTransitionsClass);
        }
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

        // Lazy capture: only walk the visual tree when the reorder target actually changed.
        // WouldChangePreview does a cheap O(1) string comparison — no visual tree traversal.
        if (!viewModel.WouldChangePreview(draggedId, target.Entry.Id, insertAfter))
            return;

        // P0: Skip bounds capture during active drag. The FLIP animation is explicitly
        // skipped during drag (ghost overlay provides feedback), and the real FLIP on
        // commit happens through OnEntriesLayoutUpdated after ApplyFilter.
        // The root selection + Capture() traverse all visual descendants and were pure waste.
        viewModel.PreviewMoveEntry(draggedId, target.Entry.Id, insertAfter);
    }

    private (LauncherEntryViewModel Entry, Control Card)? ResolveEntryCardFromDragEvent(DragEventArgs e)
    {
        var direct = MainWindowEntryCardResolver.ResolveFromSource(e.Source);
        if (direct is not null)
            return direct;

        // Check the drag card cache before falling through to visual tree traversal.
        if (_dragCardCache is not null)
        {
            var entryId = MainWindowDragEntryTransfer.TryGetEntryId(e.DataTransfer);
            if (entryId is not null && _dragCardCache.TryGetValue(entryId, out var cached))
                return cached;
        }

        return MainWindowEntryCardResolver.ResolveFromSource(this.GetVisualAt(e.GetPosition(this)));
    }

    private Dictionary<string, (LauncherEntryViewModel, Control)> BuildDragCardCache()
    {
        var cache = new Dictionary<string, (LauncherEntryViewModel, Control)>();
        foreach (var control in this.GetVisualDescendants())
        {
            if (control is Border border &&
                border.DataContext is LauncherEntryViewModel entry &&
                !string.IsNullOrWhiteSpace(entry.Id))
            {
                if (!cache.ContainsKey(entry.Id))
                    cache[entry.Id] = (entry, border);
            }
        }
        return cache;
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

        // Walk the visual tree once; share Borders between after-capture and FLIP target resolution.
        var cards = root.GetVisualDescendants().OfType<Border>().ToList();
        var after = MainWindowEntryCardBoundsCapture.Capture(
            cards,
            card => card.TranslatePoint(new Point(0, 0), this),
            card => card.Bounds.Size);
        if (after.Count == 0)
            return;

        foreach (var target in MainWindowReorderFlipAnimation.FindTargets(cards, before, after))
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

public static class MainWindowDragInsertDecision
{
    public static bool ShouldInsertAfter(Point positionInCard, Size cardSize, bool isDetailedListMode) =>
        isDetailedListMode
            ? positionInCard.Y >= cardSize.Height * 0.5
            : positionInCard.X >= cardSize.Width * 0.5;
}

public static class MainWindowDragEntryTransfer
{
    public static void SetEntryId(DataTransfer dataTransfer, string entryId)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        dataTransfer.Add(DataTransferItem.CreateText(entryId));
    }

    public static string? TryGetEntryId(IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        var entryId = LauncherEntryIds.Normalize(dataTransfer.TryGetText());
        return LauncherEntryIds.IsUsable(entryId) ? entryId : null;
    }
}
