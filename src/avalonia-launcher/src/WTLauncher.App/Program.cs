using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;

namespace WTLauncher.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Debug.WriteLine($"[WTLauncher] Unhandled domain exception: {ex}");
            Trace.WriteLine($"[WTLauncher] Unhandled domain exception: {ex}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Debug.WriteLine($"[WTLauncher] Unobserved task exception: {e.Exception}");
            Trace.WriteLine($"[WTLauncher] Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
