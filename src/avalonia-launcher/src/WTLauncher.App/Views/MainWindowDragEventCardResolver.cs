using Avalonia.Controls;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public static class MainWindowDragEventCardResolver
{
    public static (LauncherEntryViewModel Entry, Control Card)? Resolve(
        object? eventSource,
        Func<object?> hitTestFallback)
    {
        ArgumentNullException.ThrowIfNull(hitTestFallback);

        var direct = MainWindowEntryCardResolver.ResolveFromSource(eventSource);
        if (direct is not null)
            return direct;

        return MainWindowEntryCardResolver.ResolveFromSource(hitTestFallback());
    }
}
