using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace WTLauncher.App.Views;

public sealed partial class LauncherEntriesView : UserControl
{
    public LauncherEntriesView()
    {
        InitializeComponent();
    }

    public event EventHandler<RoutedEventArgs>? EntryMoreClicked;

    public event EventHandler<TappedEventArgs>? EntryRowDoubleTapped;

    public event EventHandler<PointerPressedEventArgs>? EntryDragPointerPressed;

    public ScrollViewer CardsScroll => CardsScrollViewer;

    public ItemsControl CardsItems => CardsEntriesItemsControl;

    public ListBox DetailsItems => DetailsEntriesItemsControl;

    public ScrollViewer NamesScroll => NamesScrollViewer;

    public ItemsControl NamesItems => NamesEntriesItemsControl;

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e) =>
        EntryMoreClicked?.Invoke(sender, e);

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e) =>
        EntryRowDoubleTapped?.Invoke(sender, e);

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e) =>
        EntryDragPointerPressed?.Invoke(sender, e);
}
