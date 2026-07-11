using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;
using WTLauncher.Core;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    // ── M25: Chrome measurement constants ────────────────────────────────────
    // ponytail: hardcoded layout chrome — recalibrate when the XAML layout changes.
    // Triggers: (a) outer Grid Margin in MainWindow.axaml changes; (b) header /
    // status row heights shift (added/removed controls, font size, padding);
    // (c) row gaps or border padding change. Measure live with a pixel ruler
    // after the XAML edit, then update both constants to match the new total.
    //
    // CardsRegionHorizontalChromeWidth = 48
    //   Composed of the outer Grid Margin (24 left + 24 right = 48).
    //   These 24px come from MainWindow.axaml line 280:
    //     <Grid RowDefinitions="Auto,*,Auto" Margin="24,20">
    //   If the XAML layout changes (e.g. the outer Grid margin is adjusted, or
    //   additional left/right padding is added to the card region container),
    //   update this constant to match the new total horizontal chrome.
    //
    // CardsRegionVerticalChromeHeight = 270
    //   This is the total height consumed by non-card chrome:
    //     Row 0 (header): ~230px — title, filter/sort/view/preset controls,
    //                     action buttons, plus grid margins and spacing.
    //     Row 2 (status): ~40px — status bar border with padding.
    //   Breakdown (approx):
    //     - Outer Grid margin top+bottom: 20+20 = 40
    //     - Row 0 content: ~190 (controls, buttons, spacings within header Grid)
    //     - Row 1→Row 2 gap (Margin="0,16,0,14" on card region Grid): 30
    //     - Row 2 status bar: Border Padding="16" + TextBlock height ≈ 48
    //     Total ≈ 40 + 190 + 30 + 48 = 308 → conservatively 270.
    //   The constant is intentionally slightly under the absolute chrome to ensure
    //   cards always fit within the visible area. If the header or footer layout
    //   changes significantly, recalculate with the actual measured heights.
    private const double CardsRegionHorizontalChromeWidth = 48;
    private const double CardsRegionVerticalChromeHeight = 270;
    // -1 so the first Down press moves to index 0 rather than skipping it.
    private int _keyboardFocusIndex = -1;
    private int _pendingScrollRestoreVersion;
    private readonly EventHandler<SizeChangedEventArgs>? _onSizeChangedHandler;
    private readonly EventHandler? _onOpenedHandler;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.ConfirmLaunchAsync = confirmation => new ConfirmLaunchWindow(confirmation).ShowDialog<bool>(this);
        viewModel.ConfirmBatchLaunchAsync = confirmation => new ConfirmBatchLaunchWindow(confirmation).ShowDialog<bool>(this);
        viewModel.RequestNewPresetAsync = target =>
            new PresetDetailsWindow(
                target,
                viewModel.Presets.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ShowDialog<string?>(this);
        viewModel.ConfirmDeletePresetAsync = target => new ConfirmPresetDeleteWindow(target).ShowDialog<bool>(this);
        viewModel.RequestEntryDraftAsync = entry => new EntryDetailsWindow(
            entry is not null ? new EntryDetailsWindow.EntryPrefill(entry.Id, entry.Name, entry.Directory, entry.Command, entry.Selected) : null,
            viewModel.GetPresetReferenceCount(entry?.Id ?? ""), viewModel.CommandTemplates).ShowDialog<EntryDraft?>(this);
        viewModel.ConfirmDeleteEntryAsync = target => new ConfirmEntryDeleteWindow(target).ShowDialog<bool>(this);
        viewModel.DiscoverAgentsDialogAsync = () =>
        {
            // ponytail: "already added" = first whitespace-token of any existing entry's
            // command matches a catalog exec name (case-insensitive). Survives users who
            // manually edit to full paths or add args — first token is always the exec.
            var alreadyAdded = viewModel.Entries
                .Select(e => (e.Command ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty)
                .Select(token => Path.GetFileNameWithoutExtension(token))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var vm = new DiscoverAgentsViewModel(
                () => AgentDiscoveryScanner.Scan(AgentCatalog.Entries),
                alreadyAdded);
            return new DiscoverAgentsWindow(vm).ShowDialog<IReadOnlyList<DiscoveredAgent>?>(this);
        };
        viewModel.OpenCommandManagementDialog = async applyEdit =>
        {
            var initialCommands = WTLauncher.Core.CommandUsageAnalyzer.Analyze(
                viewModel.Entries.Select(e => e.SnapshotEntry()));
            var initialTemplates = WTLauncher.Core.CommandUsageAnalyzer.AnalyzeTemplates(
                viewModel.Settings.CommandTemplates,
                viewModel.Entries.Select(e => e.SnapshotEntry()));
            var vm = new CommandManagementViewModel(initialCommands, initialTemplates, applyEdit);
            await new CommandManagementWindow(vm).ShowDialog<object?>(this);
        };
        // P6: Pointer-capture drag replaces OLE DragDrop.
        // OLE DragDrop runs a nested Win32 message loop that blocks Avalonia's render
        // scheduling. Pointer capture fires on the normal render loop, synced to display
        // refresh with no message-loop interference.
        PointerMoved += OnDragPointerMoved;
        PointerReleased += OnDragPointerReleased;
        PointerCaptureLost += OnDragPointerCaptureLost;
        _onSizeChangedHandler = (_, _) => UpdateCardRegionSize(viewModel);
        SizeChanged += _onSizeChangedHandler;
        _onOpenedHandler = (_, _) =>
        {
            UpdateCardRegionSize(viewModel);
            // Restore saved window position and state.
            var w = viewModel.Settings.Window;
            if (!double.IsNaN(w.X) && !double.IsNaN(w.Y))
            {
                Position = new PixelPoint((int)w.X, (int)w.Y);
                // Validate that the restored position is within any available screen.
                // If the saved position is off-screen (e.g. monitor disconnected),
                // move the window to the primary screen center.
                var screens = Screens.All;
                if (screens.Count > 0)
                {
                    var bounds = new PixelRect(Position, PixelSize.FromSize(new Size(Width, Height), 1));
                    var onScreen = false;
                    foreach (var screen in screens)
                    {
                        if (screen.Bounds.Intersects(bounds))
                        {
                            onScreen = true;
                            break;
                        }
                    }
                    if (!onScreen)
                    {
                        var primary = screens[0];
                        Position = new PixelPoint(
                            primary.Bounds.X + (primary.Bounds.Width - (int)Width) / 2,
                            primary.Bounds.Y + (primary.Bounds.Height - (int)Height) / 2);
                    }
                }
            }
            if (w.WindowState == "Maximized")
                WindowState = WindowState.Maximized;
        };
        Opened += _onOpenedHandler;
        Closing += OnClosing;
        KeyDown += OnWindowKeyDown;
        DisplayModeComboBox.SelectionChanged += OnDisplayModeSelectionChanged;
    }

    private void UpdateCardRegionSize(MainWindowViewModel viewModel)
    {
        // Header/footer take the stable chrome area; the view model owns the cheap grid sizing.
        viewModel.UpdateCardsAvailableSize(new Size(
            Bounds.Width - CardsRegionHorizontalChromeWidth,
            Bounds.Height - CardsRegionVerticalChromeHeight));
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
        var resolved = MainWindowEntryCardContextMenu.ResolveFromSource(source);
        if (resolved is null)
            return false;

        resolved.Value.Menu.Open(resolved.Value.Card);
        return true;
    }

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: LauncherEntryViewModel entry })
            return;

        if (!MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(e.Source))
            return;

        if (entry.LaunchCommand.CanExecute(null))
            entry.LaunchCommand.Execute(null);

        e.Handled = true;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            SearchTextBox.Focus();
            // AutoCompleteBox does not expose SelectAll(). The internal TextBox
            // is not accessible from code-behind. Focus() alone is sufficient for
            // the Ctrl+F flow — the existing text is visually selected by the user.

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ReferenceEquals(e.Source, SearchTextBox))
        {
            // When the AutoCompleteBox dropdown is closed, Enter commits the
            // current filter text to search history. When open, the
            // AutoCompleteBox consumes Enter internally for selection.
            if (!SearchTextBox.IsDropDownOpen)
                viewModel.RecordSearchTerm();
            return;
        }

        if (HandleKeyboardNavigation(e, viewModel))
            return;
    }

    private bool HandleKeyboardNavigation(KeyEventArgs e, MainWindowViewModel viewModel)
    {
        var visibleCount = viewModel.VisibleEntries.Count;

        // When there are no visible entries, reset keyboard focus state and
        // only allow Escape (clear filter). All other navigation keys must
        // return false immediately — there is nothing to navigate.
        if (visibleCount == 0)
        {
            _keyboardFocusIndex = -1;
            viewModel.KeyboardFocusedEntryId = null;

            if (e.Key == Key.Escape)
            {
                viewModel.FilterText = "";
                e.Handled = true;
                return true;
            }
            return false;
        }

        // R3.19: Revalidate keyboard focus index when visible entries change
        // (filter, sort, view mode switch). If the focused entry is no longer
        // visible, reset focus to avoid an index out of bounds.
        if (_keyboardFocusIndex >= visibleCount)
        {
            _keyboardFocusIndex = -1;
            viewModel.KeyboardFocusedEntryId = null;
        }

        var searchFocused = SearchTextBox.IsFocused;
        switch (e.Key)
        {
            case Key.Down when !searchFocused:
                _keyboardFocusIndex = Math.Min(_keyboardFocusIndex + 1, visibleCount - 1);
                viewModel.KeyboardFocusedEntryId = viewModel.VisibleEntries[_keyboardFocusIndex].Id;
                e.Handled = true;
                return true;
            case Key.Up when !searchFocused:
                _keyboardFocusIndex = Math.Max(_keyboardFocusIndex - 1, 0);
                viewModel.KeyboardFocusedEntryId = viewModel.VisibleEntries[_keyboardFocusIndex].Id;
                e.Handled = true;
                return true;
            case Key.Up when e.KeyModifiers == KeyModifiers.Control && viewModel.IsCustomSortMode:
                if (_keyboardFocusIndex >= 0 && _keyboardFocusIndex < visibleCount)
                    viewModel.VisibleEntries[_keyboardFocusIndex].MoveUpCommand.Execute(null);
                e.Handled = true;
                return true;
            case Key.Down when e.KeyModifiers == KeyModifiers.Control && viewModel.IsCustomSortMode:
                if (_keyboardFocusIndex >= 0 && _keyboardFocusIndex < visibleCount)
                    viewModel.VisibleEntries[_keyboardFocusIndex].MoveDownCommand.Execute(null);
                e.Handled = true;
                return true;
            case Key.Enter or Key.Space when !searchFocused:
                if (_keyboardFocusIndex >= 0 && _keyboardFocusIndex < visibleCount)
                {
                    var entry = viewModel.VisibleEntries[_keyboardFocusIndex];
                    if (entry.LaunchCommand.CanExecute(null))
                        entry.LaunchCommand.Execute(null);
                }
                e.Handled = true;
                return true;
            case Key.Escape:
                // Cancel any active drag — Escape is the explicit cancel gesture.
                // On platforms where Escape does NOT trigger PointerCaptureLost,
                // this is the only cancel path. ForceCleanup handles the case
                // where no drag is active (no-op).
                if (_isDragging)
                    ForceCleanup();
                viewModel.FilterText = "";
                _keyboardFocusIndex = -1;
                viewModel.KeyboardFocusedEntryId = null;
                e.Handled = true;
                return true;
        }

        return false;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            Closing -= OnClosing;
            PointerMoved -= OnDragPointerMoved;
            PointerReleased -= OnDragPointerReleased;
            PointerCaptureLost -= OnDragPointerCaptureLost;

            if (_onSizeChangedHandler is not null)
                SizeChanged -= _onSizeChangedHandler;
            if (_onOpenedHandler is not null)
                Opened -= _onOpenedHandler;
            KeyDown -= OnWindowKeyDown;
            DisplayModeComboBox.SelectionChanged -= OnDisplayModeSelectionChanged;

            if (DataContext is MainWindowViewModel viewModel)
            {
                var pos = Position;
                viewModel.TrySaveWindowBounds(
                    Width, Height,
                    x: pos.X,
                    y: pos.Y,
                    windowState: WindowState.ToString());
            }
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "OnClosing");
        }
    }

    private void OnDisplayModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // F13b: Save scroll position of the view mode being left.
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is LauncherDisplayModeViewModel oldMode)
        {
            SaveScrollOffsetForMode(oldMode.Mode, viewModel);
        }

        // F13b: Restore scroll position of the new view mode after layout completes.
        // Cancel any pending scroll-restore callback from a previous view-mode switch.
        // Without this, rapid switches queue multiple callbacks that execute with stale state.
        var restoreVersion = ++_pendingScrollRestoreVersion;
        Dispatcher.UIThread.Post(() => RestoreCurrentScrollOffset(restoreVersion), DispatcherPriority.Loaded);
    }

    private void SaveScrollOffsetForMode(LauncherEntryViewMode mode, MainWindowViewModel viewModel)
    {
        switch (mode)
        {
            case LauncherEntryViewMode.Cards:
                viewModel.SaveScrollOffset(mode, CardsScrollViewer.Offset.Y);
                break;
            case LauncherEntryViewMode.Details:
                viewModel.SaveScrollOffset(mode, GetDetailsScrollOffset());
                break;
            case LauncherEntryViewMode.Names:
                viewModel.SaveScrollOffset(mode, NamesScrollViewer.Offset.Y);
                break;
        }
    }

    private void RestoreCurrentScrollOffset(int restoreVersion)
    {
        // If a newer view-mode switch has queued a fresh restore, bail out.
        // This prevents stale callbacks from executing with outdated state.
        if (restoreVersion != _pendingScrollRestoreVersion)
            return;

        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (viewModel.IsCardsViewMode)
            CardsScrollViewer.Offset = CardsScrollViewer.Offset.WithY(viewModel.GetScrollOffset(LauncherEntryViewMode.Cards));
        else if (viewModel.IsDetailedListMode)
            SetDetailsScrollOffset(viewModel.GetScrollOffset(LauncherEntryViewMode.Details));
        else if (viewModel.IsNamesViewMode)
            NamesScrollViewer.Offset = NamesScrollViewer.Offset.WithY(viewModel.GetScrollOffset(LauncherEntryViewMode.Names));
    }

    private double GetDetailsScrollOffset()
    {
        var scrollViewer = DetailsEntriesItemsControl.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            ErrorLog.Write(
                new InvalidOperationException("FindDescendantOfType<ScrollViewer>() returned null for DetailsEntriesItemsControl."),
                "DetailsScrollOffset");
            return 0;
        }
        return scrollViewer.Offset.Y;
    }

    private void SetDetailsScrollOffset(double offset)
    {
        var scrollViewer = DetailsEntriesItemsControl.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is not null)
            scrollViewer.Offset = scrollViewer.Offset.WithY(offset);
    }

}

public static class MainWindowEntryClasses
{
    public const string Card = "card";

    public const string DragHandle = "entry-drag-handle";
}

internal static class GridExtensions
{
    public static T WithGridRow<T>(this T control, int row)
        where T : Control
    {
        Grid.SetRow(control, row);
        return control;
    }

    public static T WithGridColumn<T>(this T control, int column)
        where T : Control
    {
        Grid.SetColumn(control, column);
        return control;
    }
}

public static class MainWindowEntryRowDoubleTap
{
    public static bool ShouldLaunchFromSource(object? source)
    {
        foreach (var control in MainWindowVisualTree.ControlsFromSource(source))
        {
            if (control is Button or CheckBox)
                return false;

            if (control is Border border && border.Classes.Contains(MainWindowEntryClasses.DragHandle))
                return false;
        }

        return true;
    }
}

public static class MainWindowEntryCardContextMenu
{
    public static (ContextMenu Menu, Border Card)? ResolveFromSource(object? source)
    {
        foreach (var control in MainWindowVisualTree.ControlsFromSource(source))
        {
            if (control is not Border border || !MainWindowEntryCardResolver.IsCard(border))
                continue;

            return border.ContextMenu is ContextMenu menu
                ? (menu, border)
                : null;
        }

        return null;
    }
}

internal static class MainWindowVisualTree
{
    public static IEnumerable<Control> ControlsFromSource(object? source)
    {
        if (source is not Visual visual)
            yield break;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is Control control)
                yield return control;
        }
    }
}
