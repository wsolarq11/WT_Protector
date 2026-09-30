using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow
{
    private static readonly FontFamily GhostFontFamily = FontFamily.Parse("Cascadia Code, Consolas");
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    private IPointer? _dragPointer;
    private bool _isDragging;
    private string? _activeDragEntryId;
    private bool _lastDragInsertAfterDecision;
    // P2: Suppress XAML transitions during active drag to avoid layout thrashing.
    private const string SuppressTransitionsClass = "suppress-drag-transitions";
    private Point? _lastValidHoverPosition;
    private bool _cleaningUp;
    // Candidate drag armed on press, promoted to a real drag only after the
    // pointer travels past MainWindowDragThreshold.ThresholdPx.
    private (LauncherEntryViewModel Entry, Control Card, IPointer? Pointer, Point Start)? _pendingDrag;

    // ── Drag start (PointerPressed on the card body) ─────────────────

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            _pendingDrag = null;

            if (_isDragging)
                ForceCleanup();

            if (DataContext is not MainWindowViewModel)
                return;

            var point = e.GetCurrentPoint(this);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            // Button/CheckBox keep their own press handling; only a press on the
            // card body arms a candidate drag.
            if (!MainWindowCardGesture.IsGestureEligible(e.Source))
                return;

            var resolved = MainWindowEntryCardResolver.ResolveFromSource(sender);
            if (resolved is null)
                return;

            var (item, card) = resolved.Value;
            _pendingDrag = (item, card, e.Pointer, e.GetPosition(this));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DragPointerPressed");
            ForceCleanup();
        }
    }

    private void OnEntryDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging || _pendingDrag is not { } pending)
            return;

        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // A press promoted to a drag only while the button stays down. If the
        // button was released off-card (no PointerReleased here), the stale
        // candidate must never re-arm on a later hover.
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pendingDrag = null;
            return;
        }

        var position = e.GetPosition(this);
        if (!MainWindowDragThreshold.ShouldStartDrag(pending.Start, position))
            return;

        _pendingDrag = null;
        if (pending.Pointer is not { } pointer)
            return;

        try
        {
            BeginDrag(viewModel, pending.Entry, pending.Card, pointer, position);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DragPointerMoved");
            ForceCleanup();
        }
    }

    private void OnEntryDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingDrag = null;
    }

    private void BeginDrag(
        MainWindowViewModel viewModel,
        LauncherEntryViewModel item,
        Control card,
        IPointer pointer,
        Point position)
    {
        _activeDragEntryId = item.Id;
        _lastDragInsertAfterDecision = false;
        _lastValidHoverPosition = null;
        _dragPointer = pointer;
        _isDragging = true;

        viewModel.BeginDragReorder(item.Id);
        ShowDragGhost(item, card, position);

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
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
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
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
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
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
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

        var currentPos = e.GetPosition(this);
        var resolved = ResolveCardAtPointer(currentPos);
        if (resolved is null)
        {
            // M07: Only clear hover when pointer moves significantly away from last card
            if (_lastValidHoverPosition is not null)
            {
                var dx = currentPos.X - _lastValidHoverPosition.Value.X;
                var dy = currentPos.Y - _lastValidHoverPosition.Value.Y;
                if (Math.Abs(dx) < 20 && Math.Abs(dy) < 20)
                    return;
            }
            viewModel.ClearDragHover();
            return;
        }
        _lastValidHoverPosition = currentPos;

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
            if (entry is not null && !EntryId.SameNormalized(entry.Id, _activeDragEntryId ?? ""))
                return (entry, control);
        }
        return null;
    }

    private void ForceCleanup()
    {
        if (_cleaningUp) return;
        _cleaningUp = true;
        try
        {
        // Set _isDragging BEFORE releasing capture — Capture(null) can
        // synchronously fire OnDragPointerCaptureLost which checks this guard.
        _isDragging = false;
        _dragPointer?.Capture(null);
        _dragPointer = null;
        _activeDragEntryId = null;

        SuppressDragTransitions(false);
        HideDragGhost();
        DisplayModeComboBox.IsEnabled = true;

        if (DataContext is MainWindowViewModel vm)
            // Cancel is a no-op when the session was already cleared by Commit.
            vm.CancelDragReorder();
        }
        finally
        {
            _cleaningUp = false;
        }
    }

    // ── Ghost overlay ─────────────────────────────────────────────────

    private void ShowDragGhost(
        LauncherEntryViewModel item,
        Control card,
        Point position)
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

        var pointerX = position.X;
        var pointerY = position.Y;
        var ghost = CreateGhostBorder(
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

    private static (Border Control, double Left, double Top) CreateGhostBorder(
        string name,
        string directory,
        double titleFontSize,
        double sourceWidth,
        double sourceHeight,
        double pointerX,
        double pointerY)
    {
        var sw = double.IsFinite(sourceWidth) ? sourceWidth : 0;
        var sh = double.IsFinite(sourceHeight) ? sourceHeight : 0;
        var layout = LauncherDragGhostLayoutCalculator.Calculate(sw, sh, pointerX, pointerY);
        var control = new Border
        {
            Width = layout.Width,
            Height = layout.Height,
            Classes = { MainWindowEntryClasses.Card },
            Background = ParseBrush(LauncherDesignTokens.SurfaceColor, Brushes.White),
            BorderBrush = ParseBrush(LauncherDesignTokens.DragHighlightColor, Brushes.CornflowerBlue),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(LauncherDesignTokens.CardBorderRadius),
            Opacity = 0.86,
            IsHitTestVisible = false,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 10,
                Blur = 28,
                Spread = 0,
                Color = Color.Parse(LauncherDesignTokens.DragGhostShadowColor),
            }),
            Child = new StackPanel
            {
                Margin = new Thickness(LauncherDesignTokens.CardPadding),
                Spacing = 8,
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        FontSize = titleFontSize,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = ParseBrush(LauncherDesignTokens.TextPrimaryColor, Brushes.Black),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                    new TextBlock
                    {
                        Text = directory,
                        FontFamily = GhostFontFamily,
                        FontSize = LauncherDesignTokens.FontSizeSmall,
                        Foreground = ParseBrush(LauncherDesignTokens.TextMutedColor, Brushes.Gray),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        IsHitTestVisible = false,
                    },
                },
            },
        };

        return (control, layout.Left, layout.Top);
    }

    /// <summary>Parses a design-token color with a safe fallback so a malformed token
    /// cannot crash the static ghost factory.</summary>
    private static IBrush ParseBrush(string color, IBrush fallback)
    {
        try
        {
            return Brush.Parse(color);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            return fallback;
        }
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

public static class MainWindowDragThreshold
{
    // Windows SM_CXDRAG-style start threshold in px. Below it a press is a
    // click/double-tap; at or beyond it the press becomes a drag.
    public const double ThresholdPx = 4;

    public static bool ShouldStartDrag(Point start, Point current)
    {
        var dx = current.X - start.X;
        var dy = current.Y - start.Y;
        return (dx * dx) + (dy * dy) >= ThresholdPx * ThresholdPx;
    }
}
