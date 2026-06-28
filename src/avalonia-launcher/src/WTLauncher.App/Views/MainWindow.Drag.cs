using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow
{
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    private IPointer? _dragPointer;
    private bool _isDragging;
    private string? _activeDragEntryId;
    private bool _lastDragInsertAfterDecision;
    // P2: Suppress XAML transitions during active drag to avoid layout thrashing.
    private const string SuppressTransitionsClass = "suppress-drag-transitions";

    // ── Drag start (PointerPressed on drag handle) ───────────────────

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (_isDragging)
                ForceCleanup();

            if (DataContext is not MainWindowViewModel { IsCustomSortMode: true } viewModel)
                return;

            var point = e.GetCurrentPoint(this);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            var resolved = MainWindowEntryCardResolver.ResolveFromSource(sender);
            if (resolved is null)
                return;

            var (item, card) = resolved.Value;
            BeginDrag(viewModel, item, card, e);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "DragPointerPressed");
            ForceCleanup();
        }
    }

    private void BeginDrag(
        MainWindowViewModel viewModel,
        LauncherEntryViewModel item,
        Control card,
        PointerPressedEventArgs e)
    {
        var pointer = e.Pointer;
        if (pointer is null) return;

        _activeDragEntryId = item.Id;
        _lastDragInsertAfterDecision = false;
        _dragPointer = pointer;
        _isDragging = true;

        viewModel.BeginDragReorder(item.Id);
        ShowDragGhost(item, card, e);

        DisplayModeComboBox.IsEnabled = false;
        SuppressDragTransitions(true);

        pointer.Capture(this);
    }

    // ── PointerMoved and PointerReleased on Window ───────────────────

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _dragGhost is null)
            return;

        // ponytail: guard against ViewModel cancelling drag externally (Escape,
        // search, sort, preset, selection, entry edit, etc. all call
        // DiscardDragReorderPreview). Without this check the drag silently
        // resumes on next PointerMoved against a dead session.
        if (DataContext is MainWindowViewModel vm && !vm.HasActiveDragReorder)
        {
            ForceCleanup();
            return;
        }

        try
        {
            MoveDragGhost(e);
            ApplyDragPreview(e);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "DragPointerMoved");
        }
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        try
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.CommitDragReorder();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "DragPointerReleased");
        }
        finally
        {
            ForceCleanup();
        }
    }

    private void OnDragPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        try
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.CancelDragReorder();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "DragCaptureLost");
        }
        finally
        {
            ForceCleanup();
        }
    }

    private void ApplyDragPreview(PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;
        if (_activeDragEntryId is null)
            return;

        var resolved = ResolveCardAtPointer(e.GetPosition(this));
        if (resolved is null)
        {
            viewModel.ClearDragHover();
            return;
        }

        var (entry, card) = resolved.Value;
        var insertAfter = MainWindowDragInsertDecision.ShouldInsertAfter(
            e.GetPosition(card),
            card.Bounds.Size,
            viewModel.IsDetailedListMode,
            _lastDragInsertAfterDecision);
        _lastDragInsertAfterDecision = insertAfter;

        if (!viewModel.WouldChangePreview(_activeDragEntryId, entry.Id, insertAfter))
            return;

        viewModel.PreviewMoveEntry(_activeDragEntryId, entry.Id, insertAfter);
    }

    private (LauncherEntryViewModel Entry, Control Card)? ResolveCardAtPointer(Point position)
    {
        var visual = this.GetVisualAt(position);
        if (visual is null) return null;

        foreach (var control in MainWindowVisualTree.ControlsFromSource(visual))
        {
            var entry = MainWindowEntryCardResolver.ResolveCard(control);
            if (entry is not null && !LauncherEntryIds.SameNormalized(entry.Id, _activeDragEntryId ?? ""))
                return (entry, control);
        }
        return null;
    }

    private void ForceCleanup()
    {
        _dragPointer?.Capture(null);
        _dragPointer = null;
        _isDragging = false;
        _activeDragEntryId = null;

        SuppressDragTransitions(false);
        HideDragGhost();
        DisplayModeComboBox.IsEnabled = true;

        if (DataContext is MainWindowViewModel vm)
            vm.CancelDragReorder();
    }

    // ── Ghost overlay ─────────────────────────────────────────────────

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

        var pointerX = e.GetPosition(this).X;
        var pointerY = e.GetPosition(this).Y;
        var ghost = MainWindowDragGhostFactory.Create(
            item.Name,
            item.Directory,
            item.TitleFontSize,
            card.Bounds.Width,
            card.Bounds.Height,
            pointerX,
            pointerY);

        _dragGhost = ghost.Control;
        _overlayLayer.Children.Add(_dragGhost);
        // Clamp initial position to window bounds — MoveDragGhost reclamps on each
        // pointer move but the first frame needs it too, especially near window edges.
        var initPos = LauncherDragGhostLayoutCalculator.CalculatePosition(
            pointerX, pointerY, Bounds.Width, Bounds.Height,
            _dragGhost.Width, _dragGhost.Height);
        SetDragGhostPosition(_dragGhost, initPos.Left, initPos.Top);
    }

    private void MoveDragGhost(PointerEventArgs e)
    {
        if (_dragGhost is null) return;

        var pointerX = e.GetPosition(this).X;
        var pointerY = e.GetPosition(this).Y;
        var ghostPosition = LauncherDragGhostLayoutCalculator.CalculatePosition(
            pointerX, pointerY, Bounds.Width, Bounds.Height,
            _dragGhost.Bounds.Width, _dragGhost.Bounds.Height);
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

    // ── Transitions suppression ──────────────────────────────────────

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
}

public static class MainWindowDragInsertDecision
{
    private const double InsertThresholdDefault = 0.5;
    private const double InsertThresholdHysteresisLow = 0.45;
    private const double InsertThresholdHysteresisHigh = 0.55;

    public static bool ShouldInsertAfter(Point positionInCard, Size cardSize, bool isDetailedListMode, bool? currentInsertAfter = null)
    {
        double threshold = currentInsertAfter switch
        {
            true => InsertThresholdHysteresisLow,
            false => InsertThresholdHysteresisHigh,
            null => InsertThresholdDefault,
        };

        return isDetailedListMode
            ? positionInCard.Y >= cardSize.Height * threshold
            : positionInCard.X >= cardSize.Width * threshold;
    }
}
