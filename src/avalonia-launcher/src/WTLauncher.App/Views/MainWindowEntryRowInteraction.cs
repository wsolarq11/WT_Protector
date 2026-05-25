using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WTLauncher.App.Views;

public static class MainWindowEntryRowInteraction
{
    public static bool IsInteractiveSource(Control source)
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
}
