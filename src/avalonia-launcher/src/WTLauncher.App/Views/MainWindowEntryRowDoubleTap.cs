using Avalonia.Controls;
using Avalonia.VisualTree;

namespace WTLauncher.App.Views;

public static class MainWindowEntryRowDoubleTap
{
    public static bool ShouldLaunchFromSource(object? source)
    {
        if (source is not Control control)
            return true;

        foreach (var ancestor in control.GetSelfAndVisualAncestors())
        {
            if (ancestor is Button or CheckBox)
                return false;

            if (ancestor is Border border && border.Classes.Contains("entry-drag-handle"))
                return false;
        }

        return true;
    }
}
