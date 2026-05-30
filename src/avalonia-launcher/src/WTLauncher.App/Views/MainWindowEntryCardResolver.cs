using Avalonia.Controls;
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
        foreach (var control in MainWindowVisualTree.ControlsFromSource(source))
        {
            var entry = ResolveCard(control);
            if (entry is not null)
                return (entry, control);
        }

        return null;
    }
}
