using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class MainWindow : Window
{
    private const double CardsRegionHorizontalChromeWidth = 48;
    private const double CardsRegionVerticalChromeHeight = 270;

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
        viewModel.RequestNewPresetAsync = target => new PresetDetailsWindow(target).ShowDialog<string?>(this);
        viewModel.ConfirmDeletePresetAsync = target => new ConfirmPresetDeleteWindow(target).ShowDialog<bool>(this);
        viewModel.RequestEntryDraftAsync = entry => new EntryDetailsWindow(entry).ShowDialog<EntryDraft?>(this);
        viewModel.ConfirmDeleteEntryAsync = target => new ConfirmEntryDeleteWindow(target).ShowDialog<bool>(this);
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

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.TrySaveWindowBounds(Width, Height);
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
