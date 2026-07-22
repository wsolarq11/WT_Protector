using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WTLauncher.App.Views.Controls;

public sealed partial class NamesView : UserControl
{
    public NamesView()
    {
        InitializeComponent();
    }

    public event EventHandler<TappedEventArgs>? EntryRowDoubleTapped;

    public event EventHandler<PointerPressedEventArgs>? EntryDragPointerPressed;

    public ScrollViewer NamesScroll => NamesScrollViewer;

    public ItemsControl NamesItems => NamesEntriesItemsControl;

    private void OnEntryRowDoubleTapped(object? sender, TappedEventArgs e) =>
        EntryRowDoubleTapped?.Invoke(sender, e);

    private void OnEntryDragPointerPressed(object? sender, PointerPressedEventArgs e) =>
        EntryDragPointerPressed?.Invoke(sender, e);
}
