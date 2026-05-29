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
    private const double CardsRegionHorizontalChromeWidth = 48;
    private const double CardsRegionVerticalChromeHeight = 270;
    private string? _settingsPath;
    private LauncherSettings? _settings;
    private OverlayLayer? _overlayLayer;
    private Border? _dragGhost;
    private readonly MainWindowReorderAnimationScheduler _reorderAnimationScheduler = new();
    private string? _activeDragEntryId;

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
        var positionInCard = e.GetPosition(card);
        return viewModel.IsDetailedListMode
            ? positionInCard.Y >= (card.Bounds.Height * 0.5)
            : positionInCard.X >= (card.Bounds.Width * 0.5);
    }

    private (LauncherEntryViewModel Entry, Control Card)? ResolveEntryCardFromDragEvent(DragEventArgs e)
    {
        var direct = MainWindowEntryCardResolver.ResolveFromSource(e.Source);
        if (direct is not null)
            return direct;

        return MainWindowEntryCardResolver.ResolveFromSource(this.GetVisualAt(e.GetPosition(this)));
    }

    private void ShowDragGhost(LauncherEntryViewModel item, Control sourceCard, Point position)
    {
        _overlayLayer ??= OverlayLayer.GetOverlayLayer(this);
        if (_overlayLayer is null || _dragGhost is not null)
            return;

        var ghost = MainWindowDragGhostFactory.Create(
            item.Name,
            item.Directory,
            item.TitleFontSize,
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
        if (root is null)
            return;

        _reorderAnimationScheduler.Schedule(
            before,
            OnEntriesLayoutUpdated,
            handler => root.LayoutUpdated -= handler,
            handler => root.LayoutUpdated += handler);
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

        if (TryOpenEntryCardContextMenu(control))
            e.Handled = true;
    }

    private static bool TryOpenEntryCardContextMenu(Control source)
    {
        foreach (var ancestor in source.GetSelfAndVisualAncestors())
        {
            if (ancestor is Border border && border.Classes.Contains("card"))
            {
                if (border.ContextMenu is not ContextMenu menu)
                    return false;

                menu.Open(border);
                return true;
            }
        }

        return false;
    }

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: LauncherEntryViewModel entry })
            return;

        if (e.Source is Control source && IsInteractiveRowSource(source))
            return;

        if (entry.LaunchCommand.CanExecute(null))
            entry.LaunchCommand.Execute(null);

        e.Handled = true;
    }

    private static bool IsInteractiveRowSource(Control source)
    {
        foreach (var ancestor in source.GetSelfAndVisualAncestors())
        {
            if (ancestor is Button or CheckBox)
                return true;

            if (ancestor is Border border && border.Classes.Contains("entry-drag-handle"))
                return true;
        }

        return false;
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
