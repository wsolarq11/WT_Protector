using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;
using WTLauncher.App.Infrastructure;

namespace WTLauncher.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            var message = $"[WTLauncher] Unhandled domain exception: {ex}";
            Debug.WriteLine(message);
            Trace.WriteLine(message);
            Console.Error.WriteLine(message);
            if (ex is not null)
                ErrorLog.Write(ex, "AppDomain.UnhandledException");
            Environment.ExitCode = 2;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            var message = $"[WTLauncher] Unobserved task exception: {e.Exception}";
            Debug.WriteLine(message);
            Trace.WriteLine(message);
            Console.Error.WriteLine(message);
            ErrorLog.Write(e.Exception, "TaskScheduler.UnobservedTaskException");
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
