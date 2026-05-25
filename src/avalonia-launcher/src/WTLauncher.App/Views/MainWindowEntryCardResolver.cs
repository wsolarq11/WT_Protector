using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public static class MainWindowEntryCardResolver
{
    public static (LauncherEntryViewModel Entry, Control Card)? ResolveFromSource(object? source)
    {
        if (source is StyledElement { DataContext: LauncherEntryViewModel directEntry }
            && source is Control directControl
            && directControl.Classes.Contains("card"))
        {
            return (directEntry, directControl);
        }

        if (source is not Visual visual)
            return null;

        foreach (var ancestor in visual.GetSelfAndVisualAncestors())
        {
            if (ancestor is Control { DataContext: LauncherEntryViewModel entry } control
                && control.Classes.Contains("card"))
            {
                return (entry, control);
            }
        }

        return null;
    }
}
