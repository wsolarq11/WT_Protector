using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WTLauncher.App.Views;

public static class MainWindowCardContextMenu
{
    public static bool TryOpenFor(Control source)
    {
        return TryOpenFor(source, static (menu, card) => menu.Open(card));
    }

    public static bool TryOpenFor(Control source, Action<ContextMenu, Border> openMenu)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(openMenu);

        var card = FindCard(source);
        if (card?.ContextMenu is not ContextMenu menu)
            return false;

        openMenu(menu, card);
        return true;
    }

    public static Border? FindCard(Control source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var ancestor in source.GetSelfAndVisualAncestors())
        {
            if (ancestor is Border border && border.Classes.Contains("card"))
                return border;
        }

        return null;
    }
}
