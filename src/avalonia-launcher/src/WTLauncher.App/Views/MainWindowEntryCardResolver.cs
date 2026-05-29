using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public static class MainWindowEntryCardResolver
{
    public static bool IsCard(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.Classes.Contains("card");
    }

    public static LauncherEntryViewModel? ResolveCard(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return IsCard(control)
            ? control.DataContext as LauncherEntryViewModel
            : null;
    }

    public static (LauncherEntryViewModel Entry, Control Card)? ResolveFromSource(object? source)
    {
        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is not Control control)
                continue;

            var entry = ResolveCard(control);
            if (entry is not null)
                return (entry, control);
        }

        return null;
    }
}
