using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WTLauncher.App.Views.Controls;

public sealed partial class DetailsView : UserControl
{
    public DetailsView()
    {
        InitializeComponent();
    }

    public event EventHandler<RoutedEventArgs>? EntryMoreClicked;

    public event EventHandler<TappedEventArgs>? EntryRowDoubleTapped;

    public event EventHandler<PointerPressedEventArgs>? EntryDragPointerPressed;

    public ListBox DetailsItems => DetailsEntriesItemsControl;

    private void OnEntryMoreClicked(object? sender, RoutedEventArgs e) =>
        EntryMoreClicked?.Invoke(sender, e);

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e) =>
        EntryRowDoubleTapped?.Invoke(sender, e);

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e) =>
        EntryDragPointerPressed?.Invoke(sender, e);
}
