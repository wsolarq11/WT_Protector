using Avalonia.Controls;

namespace WTLauncher.App.Views;

public static class MainWindowEntryRowDoubleTap
{
    public static bool ShouldLaunchFromSource(object? source)
    {
        foreach (var control in MainWindowVisualTree.ControlsFromSource(source))
        {
            if (control is Button or CheckBox)
                return false;

            if (control is Border border && border.Classes.Contains(MainWindowEntryClasses.DragHandle))
                return false;
        }

        return true;
    }
}
