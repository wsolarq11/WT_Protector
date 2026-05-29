using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WTLauncher.App.Views;

public static class MainWindowEntryCardContextMenu
{
    public static (ContextMenu Menu, Border Card)? ResolveFromSource(object? source)
    {
        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is not Border border || !MainWindowEntryCardResolver.IsCard(border))
                continue;

            return border.ContextMenu is ContextMenu menu
                ? (menu, border)
                : null;
        }

        return null;
    }
}
