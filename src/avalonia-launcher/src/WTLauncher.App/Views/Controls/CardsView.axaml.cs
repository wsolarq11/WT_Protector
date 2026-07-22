using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WTLauncher.App.Views.Controls;

public sealed partial class CardsView : UserControl
{
    public CardsView()
    {
        InitializeComponent();
    }

    public event EventHandler<RoutedEventArgs>? EntryMoreClicked;

    public event EventHandler<PointerPressedEventArgs>? EntryDragPointerPressed;

    public ScrollViewer CardsScroll => CardsScrollViewer;

    public ItemsControl CardsItems => CardsEntriesItemsControl;

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e) =>
        EntryMoreClicked?.Invoke(sender, e);

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e) =>
        EntryDragPointerPressed?.Invoke(sender, e);
}
