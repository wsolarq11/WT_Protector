using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;
using WTLauncher.Core;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    // ── M25: Chrome measurement constants ────────────────────────────────────
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
    private int _keyboardFocusIndex;

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
        viewModel.RequestEntryDraftAsync = entry => new EntryDetailsWindow(entry, viewModel.GetPresetReferenceCount(entry?.Id ?? ""), viewModel.CommandTemplates).ShowDialog<EntryDraft?>(this);
        viewModel.ConfirmDeleteEntryAsync = target => new ConfirmEntryDeleteWindow(target).ShowDialog<bool>(this);
        DragDrop.AddDragOverHandler(this, OnEntryDragOver);
        DragDrop.AddDropHandler(this, OnEntryDrop);
        SizeChanged += (_, _) => UpdateCardRegionSize(viewModel);
        Opened += (_, _) =>
        {
            UpdateCardRegionSize(viewModel);
            // M11: Restore saved window position and state.
            var w = viewModel.Settings.Window;
            if (!double.IsNaN(w.X) && !double.IsNaN(w.Y))
                Position = new PixelPoint((int)w.X, (int)w.Y);
            if (w.WindowState == "Maximized")
                WindowState = WindowState.Maximized;
        };
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
            // M2: AutoCompleteBox does not expose SelectAll(). The internal TextBox
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
        if (visibleCount == 0)
        {
            if (e.Key == Key.Escape)
            {
                viewModel.FilterText = "";
                _keyboardFocusIndex = -1;
                e.Handled = true;
                return true;
            }
            return false;
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
            case Key.Enter when !searchFocused:
                if (_keyboardFocusIndex >= 0 && _keyboardFocusIndex < visibleCount)
                {
                    var entry = viewModel.VisibleEntries[_keyboardFocusIndex];
                    if (entry.LaunchCommand.CanExecute(null))
                        entry.LaunchCommand.Execute(null);
                }
                e.Handled = true;
                return true;
            case Key.Escape:
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
        if (DataContext is MainWindowViewModel viewModel)
        {
            // M11: PixelPoint is a value type — null-conditional operator (?.) is
            // not valid here. During OnClosing the window is fully materialized,
            // so Position always returns a valid PixelPoint.
            var pos = Position;
            viewModel.TrySaveWindowBounds(
                Width, Height,
                x: pos.X,
                y: pos.Y,
                windowState: WindowState.ToString());
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
        Dispatcher.UIThread.Post(RestoreCurrentScrollOffset, DispatcherPriority.Loaded);
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

    private void RestoreCurrentScrollOffset()
    {
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
            Debug.WriteLine("MainWindow: FindDescendantOfType<ScrollViewer>() returned null for DetailsEntriesItemsControl. Skipping scroll save.");
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
}

public static class MainWindowEntriesRootSelection
{
    public static Control Select(
        bool isDetailedListMode,
        bool isNamesViewMode,
        Control detailsRoot,
        Control namesRoot,
        Control cardsRoot)
    {
        ArgumentNullException.ThrowIfNull(detailsRoot);
        ArgumentNullException.ThrowIfNull(namesRoot);
        ArgumentNullException.ThrowIfNull(cardsRoot);

        if (isDetailedListMode)
            return detailsRoot;

        return isNamesViewMode ? namesRoot : cardsRoot;
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
