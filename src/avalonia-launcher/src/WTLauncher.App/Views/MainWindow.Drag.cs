using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Diagnostics;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow
{
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    // LIFECYCLE: Set in DoDragReorderAsync (drag begin) and optionally
    // overwritten in OnEntryDrop (drop finalize). Always cleared in the
    // finally block of DoDragReorderAsync. Cross-window drag not supported.
    private string? _activeDragEntryId;
    /// <summary>Last ShouldInsertAfter decision — used to provide hysteresis
    /// so the decision doesn't flip-flop near card boundaries.</summary>
    private bool _lastDragInsertAfterDecision;
    // P2: Suppress XAML transitions during active drag to avoid layout thrashing.
    private const string SuppressTransitionsClass = "suppress-drag-transitions";

    private async void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (DataContext is not MainWindowViewModel { IsCustomSortMode: true } viewModel)
                return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            var resolved = MainWindowEntryCardResolver.ResolveFromSource(sender);
            if (resolved is null)
                return;

            var (item, card) = resolved.Value;
            await DoDragReorderAsync(viewModel, item, card, e);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "DragPointerPressed");
            e.Handled = true;
        }
    }

    private async Task DoDragReorderAsync(
        MainWindowViewModel viewModel,
        LauncherEntryViewModel item,
        Control card,
        PointerPressedEventArgs e)
    {
        var data = new DataTransfer();
        MainWindowDragEntryTransfer.SetEntryId(data, item.Id);
        e.Handled = true;
        _activeDragEntryId = item.Id;
        _lastDragInsertAfterDecision = false;
        viewModel.BeginDragReorder(item.Id);
        ShowDragGhost(item, card, e);

        // R3.21: Disable display mode ComboBox during active drag.
        DisplayModeComboBox.IsEnabled = false;

        // P2: Suppress 0.12s XAML transitions on all card entries during drag.
        SuppressDragTransitions(true);

        var effect = DragDropEffects.None;
        try
        {
            effect = await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            Debug.WriteLine($"[MainWindow] Drag reorder failed: {ex}");
            ErrorLog.Write(ex, "DragReorder");
            effect = DragDropEffects.None;
        }
        finally
        {
            SuppressDragTransitions(false);
            HideDragGhost();
            FinishDragReorder(viewModel, effect);
            _activeDragEntryId = null;
            DisplayModeComboBox.IsEnabled = true;
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

        if (decision.PreviewDraggedEntryId is null || resolved is null)
            return;

        ApplyDragReorderPreview(e, viewModel, decision.PreviewDraggedEntryId, resolved.Value);
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
        if (decision.DraggedEntryId is null || resolved is null)
            return;

        ApplyDragReorderPreview(e, viewModel, decision.DraggedEntryId, resolved.Value);
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
            viewModel.IsDetailedListMode,
            _lastDragInsertAfterDecision);
        _lastDragInsertAfterDecision = insertAfter;

        // Lazy capture: only walk the visual tree when the reorder target actually changed.
        // WouldChangePreview does a cheap O(1) string comparison — no visual tree traversal.
        if (!viewModel.WouldChangePreview(draggedId, target.Entry.Id, insertAfter))
            return;

        // P0: Skip bounds capture during active drag. Ghost overlay provides visual feedback;
        // real layout update happens through ApplyFilter on commit.
        viewModel.PreviewMoveEntry(draggedId, target.Entry.Id, insertAfter);
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
        if (_overlayLayer is null)
        {
            ErrorLog.Write(
                new InvalidOperationException("OverlayLayer is null — cannot create drag ghost."),
                "DragGhost");
            return;
        }
        if (_dragGhost is not null)
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

}

public static class MainWindowDragInsertDecision
{
    public static bool ShouldInsertAfter(Point positionInCard, Size cardSize, bool isDetailedListMode, bool? currentInsertAfter = null)
    {
        // ±5% hysteresis to prevent jitter when the pointer is near the card midpoint.
        // If currently inserted after (true), the pointer must cross below 45% to flip.
        // If currently inserted before (false), the pointer must cross above 55% to flip.
        double threshold = currentInsertAfter switch
        {
            true => 0.45,
            false => 0.55,
            null => 0.5,
        };

        return isDetailedListMode
            ? positionInCard.Y >= cardSize.Height * threshold
            : positionInCard.X >= cardSize.Width * threshold;
    }
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
